using UnityEngine;
using System.Collections.Generic;
using Delaunay;
using Delaunay.Geo;

public class VoronoiDemo : MonoBehaviour
{

    public Material land;
    public const int NPOINTS = 400;
    public const int WIDTH = 1000;
    public const int HEIGHT = 1000;
	public float freqx = 0.0095f, freqy = 0.008f, offsetx = 0.63f, offsety = 0.30f;
    public GameObject road;

    public GameObject build;
    public GameObject house;

    private List<Vector2> m_points;
	private List<LineSegment> m_edges = null;
	private List<LineSegment> m_spanningTree;
	private List<LineSegment> m_delaunayTriangulation;
	private Texture2D tx;

	private float [,] createMap() 
    {
        float [,] map = new float[WIDTH, HEIGHT];
        for (int i = 0; i < WIDTH; i++)
            for (int j = 0; j < HEIGHT; j++)
                map[i, j] = Mathf.PerlinNoise(freqx * i + offsetx, freqy * j + offsety);
        return map;
    }

	void Start ()
	{
        float [,] map=createMap();
        //Color[] pixels = createPixelMap(map);

        /* Create random points for roads */
		m_points = new List<Vector2> ();
		List<uint> colors = new List<uint> ();
		for (int i = 0; i < 100; i++) {
            int x = (int) Random.Range(0, WIDTH - 1);
            int y = (int) Random.Range(0, HEIGHT - 1);
            int iter = 0;
            
            while(map[x, y] < 0.76 && iter < 10)
            {
                x = (int)Random.Range(0, WIDTH - 1);
                y = (int)Random.Range(0, HEIGHT - 1);
                iter++;
            }

            colors.Add ((uint)0);
			Vector2 vec = new Vector2(x, y); 
			m_points.Add (vec);
		}

        /* Create more random points for buildings */
        List<Vector2> build_points = new List<Vector2>();
        int total_points = 0;
        while(total_points < 500)
        {
            int x = (int)Random.Range(0, WIDTH - 1);
            int y = (int)Random.Range(0, HEIGHT - 1);
            int iter = 0;


            while (map[x, y] < 0.85 && iter < 10)
            {
                x = (int)Random.Range(0, WIDTH - 1);
                y = (int)Random.Range(0, HEIGHT - 1);
                iter++;
            }


            Vector2 new_point = new Vector2(x, y);

            if (!m_points.Contains(new_point))
            {
                build_points.Add(new_point);
                total_points++;
            }


            
        }

		/* Generate Graphs */
		Delaunay.Voronoi v = new Delaunay.Voronoi (m_points, colors, new Rect (0, 0, WIDTH, HEIGHT));
		m_edges = v.VoronoiDiagram ();
		// m_spanningTree = v.SpanningTree (KruskalType.MINIMUM);
		// m_delaunayTriangulation = v.DelaunayTriangulation ();
	

		/* Put roads*/
		Color color = Color.blue;
		for (int i = 0; i < m_edges.Count; i++) {
			LineSegment seg = m_edges [i];				
			Vector2 left = (Vector2)seg.p0;
			Vector2 right = (Vector2)seg.p1;
            Vector2 segment = (right - left) / WIDTH * 100;

            float a=Vector2.SignedAngle(Vector2.right, right-left);
            GameObject go = Instantiate(road, new Vector3(left.y/WIDTH * 10-5, 0, left.x/HEIGHT* 10-5), Quaternion.Euler(0,a+90,0));
            go.transform.localScale = new Vector3(segment.magnitude, 1, 1);
		}


        /* Put buildings */
        for (int i = 0; i < build_points.Count; i++)
        {
            Vector2 point = build_points[i];

 

            float height = map[(int)point.x, (int)point.y];

            if (height < 0.5f)
            {
                height = 0.01f;
                GameObject go = Instantiate(house, new Vector3(point.y / WIDTH * 10 - 5, 0f, point.x / HEIGHT * 10 - 5), Quaternion.identity);
                go.transform.localScale = new Vector3(0.01f, height, 0.01f);
            } else if (height < 0.75)
            {
                height *= Random.Range(2f, 4f);
                GameObject go = Instantiate(build, new Vector3(point.y / WIDTH * 10 - 5, 0f, point.x / HEIGHT * 10 - 5), Quaternion.identity);
                go.transform.localScale = new Vector3(0.01f, 0.01f * height, 0.01f);
            }
            else
            {
                height *= Random.Range(8f, 10f);
                GameObject go = Instantiate(build, new Vector3(point.y / WIDTH * 10 - 5, 0f, point.x / HEIGHT * 10 - 5), Quaternion.identity);
                go.transform.localScale = new Vector3(0.01f, 0.01f * height, 0.01f);
            }

        }

		/* Shows Delaunay triangulation */
		/*
 		color = Color.red;
		if (m_delaunayTriangulation != null) {
			for (int i = 0; i < m_delaunayTriangulation.Count; i++) {
					LineSegment seg = m_delaunayTriangulation [i];				
					Vector2 left = (Vector2)seg.p0;
					Vector2 right = (Vector2)seg.p1;
					DrawLine (pixels,left, right,color);
			}
		}*/

		/* Shows spanning tree */
		/*
		color = Color.black;
		if (m_spanningTree != null) {
			for (int i = 0; i< m_spanningTree.Count; i++) {
				LineSegment seg = m_spanningTree [i];				
				Vector2 left = (Vector2)seg.p0;
				Vector2 right = (Vector2)seg.p1;
				DrawLine (pixels,left, right,color);
			}
		}*/

		/* Apply pixels to texture */
		//tx = new Texture2D(WIDTH, HEIGHT);
        //land.SetTexture ("_MainTex", tx);
		//tx.SetPixels (pixels);
		//tx.Apply ();

	}



    /* Functions to create and draw on a pixel array */
    private Color[] createPixelMap(float[,] map)
    {
        Color[] pixels = new Color[WIDTH * HEIGHT];
        for (int i = 0; i < WIDTH; i++)
            for (int j = 0; j < HEIGHT; j++)
            {
                pixels[i * HEIGHT + j] = Color.Lerp(Color.black, Color.white, map[i, j]);
            }
        return pixels;
    }
    private void DrawPoint (Color [] pixels, Vector2 p, Color c) {
		if (p.x<WIDTH&&p.x>=0&&p.y<HEIGHT&&p.y>=0) 
		    pixels[(int)p.x*HEIGHT+(int)p.y]=c;
	}
	// Bresenham line algorithm
	private void DrawLine(Color [] pixels, Vector2 p0, Vector2 p1, Color c) {
		int x0 = (int)p0.x;
		int y0 = (int)p0.y;
		int x1 = (int)p1.x;
		int y1 = (int)p1.y;

		int dx = Mathf.Abs(x1-x0);
		int dy = Mathf.Abs(y1-y0);
		int sx = x0 < x1 ? 1 : -1;
		int sy = y0 < y1 ? 1 : -1;
		int err = dx-dy;
		while (true) {
            if (x0>=0&&x0<WIDTH&&y0>=0&&y0<HEIGHT)
    			pixels[x0*HEIGHT+y0]=c;

			if (x0 == x1 && y0 == y1) break;
			int e2 = 2*err;
			if (e2 > -dy) {
				err -= dy;
				x0 += sx;
			}
			if (e2 < dx) {
				err += dx;
				y0 += sy;
			}
		}
	}
}