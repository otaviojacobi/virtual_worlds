using UnityEngine;

public class Building
{
    public Vector3 position { get; set; }
    public Vector3 nearestPosition { get; set; }


    public GameObject instance { get; set; }


    public Building(Vector3 pos)
    {
        position = pos;
    }

    public Building(Vector3 pos, Vector2 closestPoint, GameObject go)
    {
        nearestPosition = closestPoint;
        instance = go;
    }
}