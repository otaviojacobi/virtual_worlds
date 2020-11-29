using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Delaunay.Geo;
using Delaunay;


public class Graph
{
    private Dictionary<int, List<Vector2>> inner;

    private List<Vector2> points;

    private List<LineSegment> segments;

    public Graph(List<LineSegment> edges)
    {

        inner = new Dictionary<int, List<Vector2>>();
        points = new List<Vector2>();
        segments = edges;

        for (int i = 0; i < edges.Count; i++)
        {
            Vector2 left = (Vector2)edges[i].p0;
            Vector2 right = (Vector2)edges[i].p1;

            if(!points.Contains(left)) points.Add(left);
            if(!points.Contains(right)) points.Add(right);

            int lh = properHashCode(left);
            int rh = properHashCode(right);

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

        foreach(int key in inner.Keys)
        {
            foreach(Vector2 value in inner[key])
            {
                bool found = false;
                foreach(Vector2 secondValue in inner[properHashCode(value)])
                {
                    if(properHashCode(secondValue) == key)
                    {
                        found = true;
                    }
                }
                if(found == false)
                {
                    Debug.Log("ACHEI O PULO DO GATO");

                }
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
                    bfsq.Enqueue(properHashCode(neib));
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
                if(!Q.ContainsKey(properHashCode(point)))
                {
                    Q[properHashCode(point)] = point;
                    dist[properHashCode(point)] = float.MaxValue;
                }
            }
        }

        dist[properHashCode(src)] = 0f;


        while (Q.Keys.Count > 0)
        {
            int idx = getMinDistKey(Q, dist);
            Vector2 u = Q[idx];
            Q.Remove(idx);


            foreach (Vector2 v in inner[idx])
            {
                if (Q.ContainsKey(properHashCode(v)))
                {
                    float alt = dist[idx] + Mathf.Abs(Vector2.Distance(u, v));
                    if (alt < dist[properHashCode(v)])
                    {
                        if(inner[properHashCode(u)].Contains(v))
                        {
                            dist[properHashCode(v)] = alt;
                            prev[properHashCode(v)] = u;
                        }
                    }
                }
            }
        }
        S = new List<Vector2>();

        Vector2 U = new Vector2(dest.x, dest.y);

        Debug.Assert(U.Equals(dest));

        if (prev.ContainsKey(properHashCode(U)) || U.Equals(src))
        {
            while (true)
            {
                int hash = properHashCode(U);

                if (!prev.ContainsKey(hash)) 
                    break;
                else
                {
                    S.Insert(0, U);
                    U = prev[hash];
                }
            }
        }

        for(int i = 0; i < S.Count-1; i++)
        {
            Vector2 cur = S[i];
            Vector2 nex = S[i+1];
            if(!inner[properHashCode(cur)].Contains(nex))
            {
                Debug.LogError("AHAM ");// + i + " " + S.Count);
            } else 
            {
                Debug.Log("Should log");
            }
        }

        return S;
    }


    public Vector2 GetClosestPoint(Vector2 position)
    {
        // The closest point might not be so trivial, because we want to get to road asap

        return Utils.ClosestPoint(position, points);
        /*
        // So firsts we find the closest segment to us
        LineSegment closest = Utils.GetClosestSegment(position, segments);

        // Now that we know the closests segment, we should choose between of the edges to start our search
        // Just go to the closest one :)
        float distp0 = Mathf.Abs(Vector2.Distance((Vector2)closest.p0, position));
        float distp1 = Mathf.Abs(Vector2.Distance((Vector2)closest.p1, position));

        if(distp0 < distp1) 
            return (Vector2)closest.p0;
        return (Vector2)closest.p1;
         */
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

    private int properHashCode(Vector2 vec)
    {
        return 1000 * (int) vec.x + (int) vec.y;
    }

}
