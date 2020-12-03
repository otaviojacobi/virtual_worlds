using UnityEngine;
using System.Collections.Generic;

public class Person
{
    public Vector3 workPostion { get; set; }
    public Vector3 housePosition { get; set; }

    public Building house { get; set; }
    public Building work { get; set; }

    public List<Vector2> route { get; set; }

    public GameObject carInstance { get; set; }
    public GameObject personInstance { get; set; }
    public int STATUS { get; set; }

    private float homeWorkDistance { get; set; }

    private Graph routes;

    private bool isInCar;

    private GameObject personPrefab;

    private GameObject carPrefab;

    private float wakeUpTime { get; set; }
    private float leaveWorkTime { get; set; }

    public Person(Vector3 workPostion, Vector3 housePosition, Graph routes)
    {
        this.workPostion = workPostion;
        this.housePosition = housePosition;
        this.homeWorkDistance = GetHomeWorkDistance();

        this.routes = routes;
        this.wakeUpTime = Mathf.Clamp(Utils.SampleGaussian(0.7f, 0.06f), 0.6f, 0.8f);
        this.leaveWorkTime = Mathf.Clamp(Utils.SampleGaussian(1.75f, 0.04f), 1.7f, 1.8f);

        this.isInCar = true;

        STATUS = Config.STATUS_AT_SLEEP;
        route = new List<Vector2>();
    }

    public Vector3 WorldHousePostion()
    {
        return Utils.LocalToWorld(housePosition, 0.015f);
    }

    public void Move(float speed, float dayTime, float SIMULATION_SPEED_UP)
    {

        //Debug.Log(currentEnergy);

        this.manageEnergy(dayTime, SIMULATION_SPEED_UP);

        this.checkModel();

        if (!this.IsMoving()) 
        { // arrived
            return; 
        }

        float moveStep = SIMULATION_SPEED_UP * Time.deltaTime * speed;
        Vector3 target = Utils.LocalToWorld(route[0], 0.015f);
        carInstance.transform.position = Vector3.MoveTowards(carInstance.transform.position, target, moveStep);
        personInstance.transform.position = Vector3.MoveTowards(personInstance.transform.position, target, moveStep);


        Vector3 targetDirection = target - carInstance.transform.position;
        float singleStep = SIMULATION_SPEED_UP * 5f * Time.deltaTime;

        Vector3 newDirection = Vector3.RotateTowards(carInstance.transform.forward, targetDirection, singleStep, 0.0f);
        carInstance.transform.rotation = Quaternion.LookRotation(newDirection);
        personInstance.transform.rotation = Quaternion.LookRotation(newDirection);

        if (atPoint2d(carInstance.transform.position, target))
        {
            route.RemoveAt(0);
        }
    }

    private void manageEnergy(float dayTime, float SIMULATION_SPEED_UP)
    {
        float singleStep = SIMULATION_SPEED_UP * 5f * Time.deltaTime;

        if(STATUS == Config.STATUS_AT_SLEEP)
        {
            /*
            currentEnergy += 0.4f * singleStep ;
            if(currentEnergy > movingEnergy - homeWorkDistance * 0.5f)
            {
                currentEnergy = movingEnergy;
                STATUS = Config.STATUS_AT_HOME;
            }
            */
            if(dayTime >= 0.58f && dayTime <= 0.82f) {
                if(dayTime > wakeUpTime) {
                    STATUS = Config.STATUS_AT_HOME;
                }
            }
        } 
        else if(STATUS == Config.STATUS_AT_WORK )
        {
            /*
            currentEnergy -= 0.2f * singleStep;
            if(currentEnergy <= homeWorkDistance * 0.5f)
            {
                STATUS = Config.STATUS_GO_HOME;
            }
            */
            if(dayTime >= 1.68f && dayTime <= 1.82f) {
                if(dayTime > leaveWorkTime) {
                    STATUS = Config.STATUS_GO_HOME;
                }
            }
        } 
    }

    public bool IsMoving()
    {
        return route.Count != 0;
    }

    public void GoTo(Vector2 localPos, int nextState, bool imediatelly = false)
    {
        if(imediatelly)
            route.Clear();

        route.Add(localPos);
        STATUS = nextState;
    }

    public void FollowRoadTo(Vector2 target, int nextState, bool imediatelly = false)
    {
        if(imediatelly)
            route.Clear();

        // First ensure that current point is in road graph
        Vector2 initial = GetClosestRoadPoint();
        
        // First ensure target point is in road graph
        if(!routes.HasNode(target))
        {
            target = routes.GetClosestPoint(target);
        }

        route.Add(initial);
        route.AddRange(routes.Astar(initial, target));
        route.Add(target);

        STATUS = nextState;
    }

    public Vector2 GetClosestRoadPoint()
    {
        Vector2 cur = CurrentLocalPosition();
        if(!routes.HasNode(cur))
            return routes.GetClosestPoint(cur);
        return cur;
    }

    public void DestroyInstance()
    {
        Object.Destroy(carInstance);
    }

    public Vector2 LocalWorkPosition()
    {
        return new Vector2(workPostion.z, workPostion.x);
    }

    public Vector2 LocalHousePosition()
    {
        return new Vector2(housePosition.z, housePosition.x);
    }

    public Vector2 CurrentLocalPosition()
    {
        return Utils.WorldToLocal(carInstance.transform.position);
    }

    public float GetHomeWorkDistance()
    {
        return Vector2.Distance(LocalHousePosition(), LocalHousePosition());
    }

    private bool atPoint2d(Vector3 pos1, Vector3 pos2)
    {
        Vector2 pos1_2d = new Vector2(pos1.z, pos1.x);
        Vector2 pos2_2d = new Vector2(pos2.z, pos2.x);
        return Mathf.Abs(Vector2.Distance(pos1_2d, pos2_2d)) == 0;

    }

    private void checkModel()
    {
        bool cur = isInCar;
        if(STATUS == Config.STATUS_ON_SIDEWALK || 
           STATUS == Config.STATUS_AT_WORK || 
           STATUS == Config.STATUS_TO_HOME ||
           STATUS == Config.STATUS_ENTERING_HOME)
        {
            isInCar = false;
        } else {
            isInCar = true;
        }

        if(cur != isInCar)
        {
            if(isInCar)
            {
                personInstance.SetActive(false);
                carInstance.SetActive(true);
            }
            else
            {
                personInstance.SetActive(true);
                carInstance.SetActive(false);
            }
        } 
    }
}