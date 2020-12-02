using System.Collections.Generic;
using UnityEngine;
using Delaunay.Geo;
using Priority_Queue;

public class Graph
{
    private Dictionary<int, List<Vector2>> inner;

    private Dictionary<int, Vector2> hashToVec;

    private List<LineSegment> segments;

    public Graph(List<LineSegment> edges)
    {

        inner = new Dictionary<int, List<Vector2>>();
        hashToVec = new Dictionary<int, Vector2>();
        segments = edges;

        for (int i = 0; i < edges.Count; i++)
        {
            Vector2 left = (Vector2)edges[i].p0;
            Vector2 right = (Vector2)edges[i].p1;

            int lh = Utils.HashVector2(left);
            int rh = Utils.HashVector2(right);

            hashToVec[lh] = left;
            hashToVec[rh] = right;

            if (!inner.ContainsKey(lh))
            {
                inner[lh] = new List<Vector2>();
                inner[lh].Add(right);
            }
            else
            {
                inner[lh].Add(right);
            }


            if (!inner.ContainsKey(rh))
            {
                inner[rh] = new List<Vector2>();
                inner[rh].Add(left);
            }
            else
            {
                inner[rh].Add(left);
            }
        }

        Debug.Assert(IsConnected());

    }

    /* Runs a BFS search to check if graph is fully connected */
    public bool IsConnected() 
    {
        Queue<int> bfsq = new Queue<int>();
        HashSet<int> seen = new HashSet<int>();

        List<int> keys = new List<int>(inner.Keys);
        int key = keys[Random.Range(0, keys.Count)];

        bfsq.Enqueue(key);
        while(bfsq.Count > 0) 
        {
            key = bfsq.Dequeue();

            if(!seen.Contains(key)) 
            {
                seen.Add(key);

                foreach(Vector2 neib in inner[key])
                {
                    bfsq.Enqueue(Utils.HashVector2(neib));
                }
            }
        }

        return seen.Count == inner.Count;
    }

    public List<Vector2> Dijkstra(Vector2 src, Vector2 dest)
    {

        Dictionary<int, Vector2> Q = new Dictionary<int, Vector2>();
        Dictionary<int, float> dist = new Dictionary<int, float>();
        Dictionary<int, Vector2> prev = new Dictionary<int, Vector2>();
        List<Vector2> S = new List<Vector2>();

        foreach (List<Vector2> curPoints in inner.Values)
        {
            foreach(Vector2 point in curPoints)
            {
                if(!Q.ContainsKey(Utils.HashVector2(point)))
                {
                    Q[Utils.HashVector2(point)] = point;
                    dist[Utils.HashVector2(point)] = float.MaxValue;
                }
            }
        }

        dist[Utils.HashVector2(src)] = 0f;


        while (Q.Keys.Count > 0)
        {
            int idx = getMinDistKey(Q, dist);
            Vector2 u = Q[idx];
            Q.Remove(idx);


            foreach (Vector2 v in inner[idx])
            {
                if (Q.ContainsKey(Utils.HashVector2(v)))
                {
                    float alt = dist[idx] + Mathf.Abs(Vector2.Distance(u, v));
                    if (alt < dist[Utils.HashVector2(v)])
                    {
                        if(inner[Utils.HashVector2(u)].Contains(v))
                        {
                            dist[Utils.HashVector2(v)] = alt;
                            prev[Utils.HashVector2(v)] = u;
                        }
                    }
                }
            }
        }
        S = new List<Vector2>();
        Vector2 U = new Vector2(dest.x, dest.y);

        if (prev.ContainsKey(Utils.HashVector2(U)) || U.Equals(src))
        {
            while (true)
            {
                int hash = Utils.HashVector2(U);

                if (!prev.ContainsKey(hash)) 
                    break;
                else
                {
                    S.Insert(0, U);
                    U = prev[hash];
                }
            }
        }

        return S;
    }

    public List<Vector2> Astar(Vector2 start, Vector2 goal)
    {
        SimplePriorityQueue<int> openSet = new SimplePriorityQueue<int>();
        Dictionary<int, Vector2> cameFrom = new Dictionary<int, Vector2>();
        Dictionary<int, float> gScore = new Dictionary<int, float>();
        Dictionary<int, float> fScore = new Dictionary<int, float>();



        foreach(int key in inner.Keys)
        {
            gScore[key] = float.MaxValue;
            fScore[key] = float.MaxValue;
        }
        int startHash = Utils.HashVector2(start);
        int goalHash = Utils.HashVector2(goal);

        gScore[startHash] = 0f;
        fScore[startHash] = h(start, goal);

        openSet.Enqueue(startHash, fScore[startHash]);

        while(openSet.Count > 0)
        {
            int current = openSet.First;

            if(current == goalHash)
            {
                return reconstructPath(cameFrom, current);
            }
            
            openSet.Dequeue();

            foreach(Vector2 neib in inner[current])
            {
                // d = h :) 
                float tentativeGScore = gScore[current] + h(hashToVec[current], neib);
                int hashNeib = Utils.HashVector2(neib);
                if(tentativeGScore < gScore[hashNeib])
                {
                    cameFrom[hashNeib] = hashToVec[current];
                    gScore[hashNeib] = tentativeGScore;
                    fScore[hashNeib] = gScore[hashNeib] + h(start, neib);

                    if(!openSet.Contains(hashNeib))
                    {
                        openSet.Enqueue(hashNeib, fScore[hashNeib]);
                    }
                }
            }
        }

        Debug.LogWarning("Astar failed to find a path");
        return null;
    }

    private List<Vector2> reconstructPath(Dictionary<int, Vector2> cameFrom, int current)
    {
        List<Vector2> path = new List<Vector2>();
        while(cameFrom.ContainsKey(current))
        {   
            Vector2 next = cameFrom[current];
            current = Utils.HashVector2(next);
            path.Insert(0, next);
        }
        return path;
    }

    private float h(Vector2 dest, Vector2 n)
    {
        return Mathf.Abs(Vector2.Distance(dest, n));
    }

    public Vector2 GetClosestPoint(Vector2 position)
    {
        // The closest point might not be so trivial, because we want to get to road asap

        //return Utils.ClosestPoint(position, points);

        // So firsts we find the closest segment to us
        LineSegment closest = Utils.GetClosestSegment(position, segments);

        // Now that we know the closests segment, we should choose between of the edges to start our search
        // Just go to the closest one :)
        float distp0 = Mathf.Abs(Vector2.Distance((Vector2)closest.p0, position));
        float distp1 = Mathf.Abs(Vector2.Distance((Vector2)closest.p1, position));

        if(distp0 < distp1) 
            return (Vector2)closest.p0;
        return (Vector2)closest.p1;
    }

    public bool HasNode(Vector2 node)
    {
        return inner.ContainsKey(Utils.HashVector2(node));
    }

    private int getMinDistKey(Dictionary<int, Vector2> Q, Dictionary<int, float> dist)
    {
        float min = float.MaxValue;
        int finalKey = 0;

        foreach (int key in Q.Keys)
        {
            if (dist[key] < min)
            {
                min = dist[key];
                finalKey = key;
            }
        }

        if (min == float.MaxValue)
        {
            Debug.LogWarning("Couldnt find value in dict");
        }

        return finalKey;
    }

}
