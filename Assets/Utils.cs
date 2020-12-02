using UnityEngine;
using System.Collections.Generic;
using Delaunay.Geo;
using UnityEditor;

public class Config
{
    public static int WIDTH = 1000;
    public static int HEIGHT = 1000;

    public static float NOISE_FREQX = 0.002f;
    public static float NOISE_FREQY = 0.0018f;
    public static float NOISE_OFFSETX = 0.63f;
    public static float NOISE_OFFSETY = 0.30f;

    public static int PARK_OBJECTS = 600;

    public static int BUILDINGS_MAX = 500;

    public static int ROADS_MAX = 100;

    public static int POPULATION_SIZE = 800;

    public static float SIMULATION_SPEED_UP = 2f;

    public static int STATUS_AT_SLEEP = 0;
    public static int STATUS_AT_HOME = 1;
    public static int STATUS_ON_SIDEWALK = 3;
    public static int STATUS_ON_ROAD = 4;
    public static int STATUS_AT_WORK = 5;
    public static int STATUS_GO_HOME = 6;
    public static int STATUS_TO_HOME = 7;
    public static int STATUS_WAY_HOME = 8;
    public static int STATUS_ENTERING_HOME = 9;
}

public class Utils
{

    /* Converts from our local map coords to unity world coords */
    public static Vector3 LocalToWorld(Vector2 localCoord, float yCoord = 0f)
    {
        return new Vector3(localCoord.y / Config.WIDTH * 10 - 5f, yCoord, localCoord.x / Config.HEIGHT * 10 - 5f);
    }

    /* Converts from our local map when with Vector3 coords to unity world coords */
    public static Vector3 LocalToWorld(Vector3 localCoord, float yCoord = 0f)
    {
        return new Vector3(localCoord.x / Config.WIDTH * 10 - 5f, yCoord, localCoord.z / Config.HEIGHT * 10 - 5f);
    }

    /* Converts from unity world coords  to our local map coords */
    public static Vector2 WorldToLocal(Vector3 worldCoord)
    {
        return new Vector2(Config.WIDTH / 10f * (worldCoord.z + 5f), Config.HEIGHT / 10f * (worldCoord.x + 5f));
    }

    /* Given a list of points in order representing a closed polygon, calculate the area within  */
    public static float PolygonArea(List<Vector2> polygon)
    {
        float s = polygon[0].y * (polygon[polygon.Count - 1].x - polygon[1].x);


        for (int i = 1; i < polygon.Count; i++)
        {
            s += polygon[i].y * (polygon[i - 1].x - polygon[(i + 1) % polygon.Count].x);
        }
        return Mathf.Abs(s / 2);
    }

    /* Given an origin point and a list of segments, find the closest point to the origin point which is in one of these segments */
    public static Vector2 ClosestInLine(Vector2 point, List<LineSegment> segments)
    {
        float min = float.MaxValue;
        LineSegment closest = segments[0];

        /* First, find the closest segment */
        for (int i = 0; i < segments.Count; i++)
        {
            float d = HandleUtility.DistancePointToLineSegment(point, (Vector2)segments[i].p1, (Vector2)segments[i].p0);
            if (d < min)
            {
                min = d;
                closest = segments[i];
            }
        }

        /* Then find the closest point to our origin in this segment */
        return ClosestPointToSegment(point, (Vector2)closest.p1, (Vector2)closest.p0);
    }

    /* Given a point P and a segment (represented as points p0(A) and p1(B), find the closest point in this segment to the point P */
    public static Vector2 ClosestPointToSegment(Vector2 P, Vector2 A, Vector2 B)
    {
        Vector2 AP = P - A;       //Vector from A to P   
        Vector2 AB = B - A;       //Vector from A to B  

        float magnitudeAB = AB.sqrMagnitude;     //Magnitude of AB vector (it's length squared)     
        float ABAPproduct = Vector2.Dot(AP, AB);    //The DOT product of a_to_p and a_to_b     
        float distance = ABAPproduct / magnitudeAB; //The normalized "distance" from a to your closest point  

        if (distance < 0)     //Check if P projection is over vectorAB     
        {
            return A;

        }
        else if (distance > 1)
        {
            return B;
        }
        else
        {
            return A + AB * distance;
        }
    }

    /* Given a origin point and a list of points, finds the point in the list closest to the origin point*/
    public static Vector2 ClosestPoint(Vector2 pos, List<Vector2> points)
    {
        Vector2 closest = points[0];
        float min = float.MaxValue;
        for (int i = 0; i < points.Count; i++)
        {
            float dist = Vector2.Distance(pos, points[i]);
            if (dist < min)
            {
                min = dist;
                closest = points[i];
            }
        }

        return new Vector2(closest.x, closest.y);
    }

    /* Given a point and a polygon (list of ordered points), checks if the point is inside the polygon */
    public static bool IsPointInPolygon(Vector2 point, List<Vector2> polygon)
    {
        int polygonLength = polygon.Count, i = 0;
        bool inside = false;
        // x, y for tested point.
        float pointX = point.x, pointY = point.y;
        // start / end point for the current polygon segment.
        float startX, startY, endX, endY;
        Vector2 endPoint = polygon[polygonLength - 1];
        endX = endPoint.x;
        endY = endPoint.y;
        while (i < polygonLength)
        {
            startX = endX; startY = endY;
            endPoint = polygon[i++];
            endX = endPoint.x; endY = endPoint.y;
            //
            inside ^= (endY > pointY ^ startY > pointY) /* ? pointY inside [startY;endY] segment ? */
                      && /* if so, test if it is under the segment */
                      ((pointX - endX) < (pointY - endY) * (startX - endX) / (startY - endY));
        }
        return inside;
    }

    /* checks if a given point is inside the segment with given tolerancy */
    public static bool IsPointInSegments(Vector2 point, List<LineSegment> segments, float tolerancy = 0f)
    {
        for (int i = 0; i < segments.Count; i++)
        {
            float dist = HandleUtility.DistancePointToLineSegment(point, (Vector2)segments[i].p1, (Vector2)segments[i].p0);
            if (dist < tolerancy) return true;
        }
        return false;
    }

    /* from a given point, returns the closest segment from a list of it */
    public static LineSegment GetClosestSegment(Vector2 point, List<LineSegment> segments)
    {
        float min = float.MaxValue;
        LineSegment closerSegment = segments[0];

        for (int i = 0; i < segments.Count; i++)
        {
            float dist = HandleUtility.DistancePointToLineSegment(point, (Vector2)segments[i].p1, (Vector2)segments[i].p0);

            if(dist < min)
            {
                min = dist;
                closerSegment = segments[i];
            }
        }
        return closerSegment;
    }


    /* Checks if a point is near any of the points in a list of points with given tolerancy*/
    public static bool IsPointClose(Vector2 point, List<Vector2> points, float tolerancy = 0f)
    {
        for (int i = 0; i < points.Count; i++)
        {
            if (Vector2.Distance(point, points[i]) < tolerancy)
            {
                return true;
            }
        }
        return false;

    }

    public static float SampleGaussian(float mean, float stddev)
    {
        float x1 = 1 - Random.Range(0f, 1f);
        float x2 = 1 - Random.Range(0f, 1f);

        float y1 = Mathf.Sqrt(-2f * Mathf.Log(x1)) * Mathf.Cos(2f * Mathf.PI * x2);
        return y1 * stddev + mean;
    }

    public static int HashVector2(Vector2 vec)
    {
        return 1000 * (int) vec.x + (int) vec.y;
    }

    public static int HashVector3(Vector3 vec)
    {
        float hash = 2000000f *  vec.x +  (int) 2000f * vec.y + (int) vec.z;
        return (int) hash;
    }

}
