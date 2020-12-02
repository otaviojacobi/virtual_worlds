using UnityEngine;

public class Building
{
    public Vector3 position { get; set; }
    public Vector2 nearestPosition { get; set; }
    private bool lightsOn;

    private static Texture2D lightedBuildTexture = (Texture2D)Resources.Load("lightedText");
    private static Texture2D offTexture = (Texture2D)Resources.Load("offTexture");

    public GameObject instance { get; set; }


    public Building(Vector3 pos)
    {
        position = pos;
        lightsOn = false;
    }

    public Building(Vector3 pos, Vector2 closestPoint, GameObject go)
    {
        nearestPosition = closestPoint;
        instance = go;
        lightsOn = false;
    }

    public void TurnLightsOn()
    {
        if(!lightsOn)
        {
            lightsOn = true;
            instance.GetComponent<Renderer>().material.mainTexture = lightedBuildTexture;
        }
    }

    public void TurnLightsOff()
    {
        if(lightsOn)
        {
            lightsOn = false;
            instance.GetComponent<Renderer>().material.mainTexture = offTexture;

        }
    }
}