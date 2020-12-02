using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using Delaunay.Geo;

public class VoronoiDemo : MonoBehaviour
{

    public GameObject Road;
    public GameObject Build;
    public GameObject House;
    public GameObject SideWalk;
    public GameObject Tree;
    public GameObject Bench;
    public GameObject Bush;
    public GameObject BlueCar;
    public GameObject GreenCar;
    public GameObject PinkCar;
    public GameObject BrownCar;
    public GameObject PersonPrefab;


    private List<LineSegment> normalizedEdges;
    private List<Vector2> roadPoints;

    private List<Vector2> firstParkRegion;
    private List<Vector2> secondParkRegion;
    private List<Vector2> thirdParkRegion;

    private Delaunay.Voronoi v;


    private float[,] map;

    private List<Vector2> buildPoints;
    private List<Vector2> parkPoints;
    private float dayTime;
    private Dictionary<int, Building> allBuildings;
    private List<Vector3> workingBuildingsCoords;
    private List<Vector3> housingBuildingsCoords;

    private List<Vector3> freeHousingCoords;
    private List<Vector3> freeWorkingCoords;

    private List<Person> population;

    private Graph graph;

    Camera mainCamera;


    void Start()
    {
        Random.InitState(4);

        mainCamera = Camera.main;

        map = createMap();

        /* Create random points for roads */
        roadPoints = getRoadPoints(Config.ROADS_MAX);

        /* Set up voronoi with previous road points */
        setUpVoronoi();

        /* Set up the three parks exclusive areas */
        setUpParkRegions(Config.PARK_OBJECTS);

        /* Create more random points for buildings */
        buildPoints = getBuildPoints(Config.BUILDINGS_MAX);

        /* Put roads using roadPoints */
        instantiateRoads();

        /* Put parks objects */
        instantiateParks();

        /* Put buildings using buildPoints */
        instantiateBuildings();

        dayTime = 0.7f;

        /* Create our bon hommes. *NOTE*: always call this last :) */
        createPopulation(Config.POPULATION_SIZE);


        mainCamera.transform.position = population[0].WorldHousePostion();
    }


    void Update()
    {
        movePeople();
    }

    private float[,] createMap()
    {
        float[,] map = new float[Config.WIDTH, Config.HEIGHT];
        for (int i = 0; i < Config.WIDTH; i++)
            for (int j = 0; j < Config.HEIGHT; j++)
                map[i, j] = Mathf.PerlinNoise(Config.NOISE_FREQX * i + Config.NOISE_OFFSETX, Config.NOISE_FREQY * j + Config.NOISE_OFFSETY);
        return map;
    }

    private List<Vector2> getRoadPoints(int maximumPoints)
    {
        List<Vector2> pts = new List<Vector2>();

        for (int i = 0; i < maximumPoints; i++)
        {
            int x = (int)Random.Range(0, Config.WIDTH - 1);
            int y = (int)Random.Range(0, Config.HEIGHT - 1);
            int iter = 0;

            while (map[x, y] < 0.7 && iter < 10)
            {
                x = (int)Random.Range(0, Config.WIDTH - 1);
                y = (int)Random.Range(0, Config.HEIGHT - 1);
                iter++;
            }

            Vector2 vec = new Vector2(x, y);
            pts.Add(vec);
        }

        return pts;
    }

    private void setUpVoronoi()
    {
        /* Create road graph */
        List<uint> colors = new List<uint>();
        for (int i = 0; i < roadPoints.Count; i++) colors.Add((uint)0);
        v = new Delaunay.Voronoi(roadPoints, colors, new Rect(0, 0, Config.WIDTH, Config.HEIGHT));
        List<LineSegment> m_edges = v.VoronoiDiagram();

        normalizedEdges = new List<LineSegment>();

        for (int i = 0; i < m_edges.Count; i++)
        {
            Vector2 p0 = (Vector2)m_edges[i].p0;
            Vector2 p1 = (Vector2)m_edges[i].p1;

            p0.x = Mathf.Round(p0.x);
            p0.y = Mathf.Round(p0.y);
            p1.x = Mathf.Round(p1.x);
            p1.y = Mathf.Round(p1.y);

            normalizedEdges.Add(new LineSegment(p0, p1));
        }

        graph = new Graph(normalizedEdges);
    }

    private void setUpParkRegions(int objects)
    {
        float maxArea = float.MinValue;
        for (int i = 0; i < roadPoints.Count; i++)
        {
            List<Vector2> region = v.Region(roadPoints[i]);
            float area = Utils.PolygonArea(region);

            if (area > maxArea)
            {
                maxArea = area;
                thirdParkRegion = secondParkRegion;
                secondParkRegion = firstParkRegion;
                firstParkRegion = region;
            }
        }

        parkPoints = new List<Vector2>();

        int x, y, iter, j = 0;

        Vector2 point;


        while (j < objects)
        {
            x = (int)Random.Range(0, Config.WIDTH - 1);
            y = (int)Random.Range(0, Config.HEIGHT - 1);

            point = new Vector2(x, y);

            iter = 0;
            while (!inParkRegion(point) || Utils.IsPointInSegments(point, normalizedEdges, 5f) && iter < 100)
            {
                x = (int)Random.Range(0, Config.WIDTH - 1);
                y = (int)Random.Range(0, Config.HEIGHT - 1);
                point = new Vector2(x, y);
                iter++;
            }

            parkPoints.Add(point);
            j++;
        }
    }

    private List<Vector2> getBuildPoints(int maximumPoints)
    {

        List<Vector2> pts = new List<Vector2>();
        int total_points = 0;

        Vector2 newPoint;
        bool insidePark;

        while (total_points < maximumPoints)
        {
            int x = (int)Random.Range(0, Config.WIDTH - 1);
            int y = (int)Random.Range(0, Config.HEIGHT - 1);
            int iter = 0;


            while (map[x, y] < 0.80 && iter < 10)
            {
                x = (int)Random.Range(0, Config.WIDTH - 1);
                y = (int)Random.Range(0, Config.HEIGHT - 1);
                iter++;
            }
            iter = 0;


            insidePark = false;
            while (true)
            {

                x = (int)Random.Range(0, Config.WIDTH - 1);
                y = (int)Random.Range(0, Config.HEIGHT - 1);

                newPoint = new Vector2(x, y);

                float height = map[x, y];

                float road_tolerancy = 7f;
                float in_between_tolerancy = 30f;

                if (height > 0.75) // big buildings can be near the roads
                {
                    road_tolerancy = 5f;
                    in_between_tolerancy = 20f;
                }

                insidePark = inParkRegion(newPoint);

                if ((!insidePark && !Utils.IsPointClose(newPoint, pts, in_between_tolerancy) && !Utils.IsPointInSegments(newPoint, normalizedEdges, road_tolerancy)) || iter >= 200)
                {
                    break;
                }
                iter++;
            }

            if (iter < 200 && !insidePark)
            {
                pts.Add(newPoint);

            }
            total_points++;
        }
        return pts;

    }


    private void instantiateRoads()
    {
        for (int i = 0; i < normalizedEdges.Count; i++)
        {
            LineSegment seg = normalizedEdges[i];
            Vector2 left = (Vector2)seg.p0;
            Vector2 right = (Vector2)seg.p1;

            //Debug.DrawLine(new Vector3(left.x, 0.2f, left.y), new Vector3(right.x, 0.2f, left.y), Color.red, float.MaxValue, false);

            Vector2 segment = (right - left) / Config.WIDTH * 100;

            float a = Vector2.SignedAngle(Vector2.right, right - left);
            GameObject go = Instantiate(Road, Utils.LocalToWorld(left), Quaternion.Euler(0, a + 90, 0));
            go.transform.localScale = new Vector3(segment.magnitude, 1, 1);
        }
    }

    private void instantiateParks()
    {
        GameObject go;
        Vector3 pos;
        for (int i = 0; i < parkPoints.Count; i++)
        {
            float prob = (float)Random.Range(0f, 1f);
            pos = Utils.LocalToWorld(parkPoints[i]);

            if (prob < 0.2)
            {
                go = Instantiate(Bench, pos, Quaternion.Euler(0, Random.Range(0, 360), 0));
                go.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
            }
            else if (prob < 0.4)
            {
                go = Instantiate(Bush, pos, Quaternion.identity);
                go.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
            }
            else
            {
                go = Instantiate(Tree, pos, Quaternion.identity);
                float h = (float)Random.Range(0.02f, 0.03f);
                go.transform.localScale = new Vector3(0.02f, h, 0.02f);
            }

        }
    }

    private void instantiateSideWalk(Vector2 start, Vector2 end, float angle)
    {
        Vector2 movedPoint = Vector2.MoveTowards(end, start, 2.5f);
        Vector2 segment = (start - movedPoint) / Config.WIDTH * 100;

        GameObject go = Instantiate(SideWalk, Utils.LocalToWorld(movedPoint), Quaternion.Euler(0, angle, 0));
        go.transform.localScale = new Vector3(segment.magnitude, 1, 0.3f);
    }

    private void instantiateBuildings()
    {
        allBuildings = new Dictionary<int, Building>();
        housingBuildingsCoords = new List<Vector3>();
        workingBuildingsCoords = new List<Vector3>();

        Vector2 point, closestPoint;
        float height, roadDirection;
        GameObject go;
        Quaternion rot;

        for (int i = 0; i < buildPoints.Count; i++)
        {

            point = buildPoints[i];
            height = map[(int)point.x, (int)point.y];

            closestPoint = Utils.ClosestInLine(point, normalizedEdges);
            roadDirection = Vector2.SignedAngle(Vector2.right, point - closestPoint) + 90;

            instantiateSideWalk(point, closestPoint, roadDirection);

            rot = Quaternion.Euler(0, roadDirection, 0);

            if (height < 0.5f)
            {
                go = Instantiate(House, Utils.LocalToWorld(point), rot);
                go.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

                Vector3 buildPos = new Vector3(point.y, 0, point.x);
                allBuildings[Utils.HashVector3(buildPos)] = new Building(buildPos, closestPoint, go);
                housingBuildingsCoords.Add(buildPos);
            }
            else if (height < 0.75)
            {
                height *= Random.Range(2f, 4f);
                go = Instantiate(Build, Utils.LocalToWorld(point), rot);
                go.transform.localScale = new Vector3(0.01f, 0.01f * height, 0.01f);

                Vector3 buildPos = new Vector3(point.y, 0, point.x);
                allBuildings[Utils.HashVector3(buildPos)] = new Building(buildPos, closestPoint, go);
                housingBuildingsCoords.Add(buildPos);

            }
            else
            {
                height *= Random.Range(8f, 10f);

                int floors = (int)height;

                for (int cur_floor = 0; cur_floor < floors; cur_floor++)
                {
                    go = Instantiate(Build, Utils.LocalToWorld(point, 0.1f * cur_floor), rot);
                    go.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

                    Vector3 buildPos = new Vector3(point.y, 0.1f * cur_floor, point.x);
                    allBuildings[Utils.HashVector3(buildPos)] = new Building(buildPos, closestPoint, go);
                    workingBuildingsCoords.Add(buildPos);

                }
            }
        }

        freeHousingCoords = housingBuildingsCoords.ConvertAll(house => new Vector3(house.x, house.y, house.z));
        freeWorkingCoords = workingBuildingsCoords.ConvertAll(house => new Vector3(house.x, house.y, house.z));

    }

    private bool inParkRegion(Vector2 point)
    {
        return Utils.IsPointInPolygon(point, firstParkRegion) || Utils.IsPointInPolygon(point, secondParkRegion) || Utils.IsPointInPolygon(point, thirdParkRegion);
    }

    private Vector3 selectRandomHouse()
    {

        // Not enough houses left, repeat the houses
        if (freeHousingCoords.Count == 0)
        {
            return housingBuildingsCoords[Random.Range(0, housingBuildingsCoords.Count)];
        } else {
            int houseIdx = Random.Range(0, freeHousingCoords.Count);
            Vector3 house = freeHousingCoords[houseIdx];
            freeHousingCoords.RemoveAt(houseIdx);
            return house;
        }
    }

    private Vector3 selectRandomJob()
    {
        // Not enough working places left, repeat the working places
        if(freeWorkingCoords.Count == 0)
        {
            return workingBuildingsCoords[Random.Range(0, workingBuildingsCoords.Count)];
        } else 
        {
            int workIdx = Random.Range(0, freeWorkingCoords.Count);
            Vector3 work = freeWorkingCoords[workIdx];
            freeWorkingCoords.RemoveAt(workIdx);
            return work;
        }
    }

    private void createPopulation(int populationSize)
    {
        population = new List<Person>();

        for (int i = 0; i < populationSize; i++)
        {
            GameObject car = getRandomCar();
            Person p = new Person(selectRandomJob(), selectRandomHouse(), graph);
            Building house = allBuildings[Utils.HashVector3(p.housePosition)];
            Building work = allBuildings[Utils.HashVector3(p.workPostion)];
            p.house = house;
            p.work = work;
            house.TurnLightsOn();

            p.carInstance = Instantiate(car, p.WorldHousePostion(), Quaternion.Euler(0, house.instance.transform.rotation.eulerAngles.y + 90, 0));
            p.carInstance.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);

            p.personInstance = Instantiate(PersonPrefab, p.WorldHousePostion(), Quaternion.Euler(0, house.instance.transform.rotation.eulerAngles.y + 90, 0));
            p.personInstance.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);

            population.Add(p);
        }
    }

    private GameObject getRandomCar() {
        float prob = (float)Random.Range(0f, 1f);

        if(prob < 0.25f) return BlueCar;
        if(prob < 0.5f) return BrownCar;
        if(prob < 0.75f) return PinkCar;
        return GreenCar;
    }

    private void movePeople()
    {
        Debug.Log(dayTime);
        for (int i = 0; i < population.Count; i++)
        {

            Person p = population[i];
            float speed = 0.3f;

            // If at home, then first go to the nearest road
            if (p.STATUS == Config.STATUS_AT_HOME)
            {
                p.house.TurnLightsOff();
                Vector2 routePoint = p.house.nearestPosition;
                p.GoTo(routePoint, Config.STATUS_ON_SIDEWALK);
                speed = 0.035f;
            }
            else if (p.STATUS == Config.STATUS_ON_SIDEWALK)
            {
                speed = 0.035f;
                if(!p.IsMoving()) 
                {
                    p.FollowRoadTo(p.LocalWorkPosition(), Config.STATUS_ON_ROAD);
                    Vector2 routePoint = p.work.nearestPosition;
                    p.GoTo(routePoint, Config.STATUS_ON_ROAD);
                }
            }
            else if(p.STATUS == Config.STATUS_ON_ROAD) 
            {
                if(!p.IsMoving()) 
                {
                    p.GoTo(new Vector2(p.workPostion.z, p.workPostion.x), Config.STATUS_AT_WORK);
                    speed = 0.035f;
                }
            }
            else if(p.STATUS == Config.STATUS_AT_WORK)
            {
                speed = 0.035f;
                if(!p.IsMoving()) 
                {
                    p.work.TurnLightsOn();
                }
            }
            else if(p.STATUS == Config.STATUS_GO_HOME)
            {
                if(!p.IsMoving()) 
                {
                    p.work.TurnLightsOff();
                    Vector2 routePoint = p.work.nearestPosition;
                    p.GoTo(routePoint, Config.STATUS_TO_HOME);
                    speed = 0.035f;
                }
            }
            else if(p.STATUS == Config.STATUS_TO_HOME)
            {
                speed = 0.035f;
                if(!p.IsMoving()) 
                {
                    p.FollowRoadTo(p.LocalHousePosition(), Config.STATUS_WAY_HOME);
                    Vector2 routePoint = p.house.nearestPosition;
                    p.GoTo(routePoint, Config.STATUS_WAY_HOME);
                }
            }
            else if(p.STATUS == Config.STATUS_WAY_HOME)
            {
                if(!p.IsMoving()) 
                {
                    p.GoTo(new Vector2(p.housePosition.z, p.housePosition.x), Config.STATUS_ENTERING_HOME);
                    speed = 0.035f;
                }
            }
            else if(p.STATUS == Config.STATUS_ENTERING_HOME)
            {
                speed = 0.035f;
                if(!p.IsMoving()) 
                {
                    p.house.TurnLightsOn();
                    p.STATUS = Config.STATUS_AT_SLEEP;
                }
            }

            if(dayTime >= 2.4f) 
            {
                dayTime = 0f;
            }
            dayTime += Config.SIMULATION_SPEED_UP * Time.deltaTime * 0.0001f / 8f;

            p.Move(speed, dayTime);
        }
    }

    private void LateUpdate()
    {

        //Transform playerTransform = population[0].instance.transform;

        //Vector3 _cameraOffset = new Vector3(0f, 0f, 0f);

        //Vector3 newPos = playerTransform.position + _cameraOffset;

        //mainCamera.transform.position = 

    }
}