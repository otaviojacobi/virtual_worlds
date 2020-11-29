using UnityEngine;
using System.Collections.Generic;


public class Person
{
    public Vector3 workPostion { get; set; }
    public Vector3 housePosition { get; set; }

    public List<Vector2> route { get; set; }

    public GameObject instance { get; set; }

    public bool onRoad { get; set; }

    private bool filled { get; set; }

    public Person(Vector3 wp, Vector3 hp)
    {
        workPostion = wp;
        housePosition = hp;
        onRoad = false;

        filled = false;
    }

    public Vector3 WorldHousePostion()
    {
        return Utils.LocalToWorld(housePosition, 0.015f);
    }

    public void FillRoute(Graph graph)
    {
        if (!filled)
        {

            Vector2 currentPosition = Utils.WorldToLocal(instance.transform.position);

            Vector2 initial = graph.GetClosestPoint(currentPosition);


            Vector2 last = graph.GetClosestPoint(new Vector2(workPostion.z, workPostion.x));

            route = graph.Dijkstra(initial, last);

            filled = true;
        }
    }

    public void Move()
    {
        if (route.Count == 0) { return; }


        Vector3 target = Utils.LocalToWorld(route[0], 0.015f);
        instance.transform.position = Vector3.MoveTowards(instance.transform.position, target, Time.deltaTime * 0.3f);


        Vector3 targetDirection = target - instance.transform.position;
        float singleStep = 5f * Time.deltaTime;

        Vector3 newDirection = Vector3.RotateTowards(instance.transform.forward, targetDirection, singleStep, 0.0f);
        instance.transform.rotation = Quaternion.LookRotation(newDirection);

        if (Mathf.Abs(Vector3.Distance(instance.transform.position, target)) == 0)
        {
            route.RemoveAt(0);
        }

    }
}