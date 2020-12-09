using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;

// Using the Marching Cubes algorithm, generate random and destructible terrain
public class Generate : MonoBehaviour
{
    // Public parameters
    public Vector3 GridSize = new Vector3(20, 10, 20);      // Size of grid
    public float Zoom = 1f;                                 // Scale of grid
    [Range(0.0f, 1.0f)]
    public float Isolevel = 0.5f;                           // Chosen isolevel threshold
    public Material[] Materials = null;                     // Available materials
    public LineRenderer GridBounds = null;                  // Visualize grid boundaries

    // Private Parameters
    private GridPoint[,,] grid = null;                      // Grid is 3D array of GridPoints
    private GridCell cell = new GridCell();                 // Current GridCell (cube) being marched
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector2> uv = new List<Vector2>();
    private Material m = null;                              // Random material to be used
    private bool buildRequest = false;                      // Outstanding build request

    // Start is called before the first frame update
    void Start()
    {
        // Select a random material to use
        m = Materials[Random.Range(0, Materials.Length)];
        InitializeGrid();
        SampleIsofield();
        ExtractIsosurface();
        DrawGridBounds();
        UnityEngine.Debug.Log("Initial generation complete");
    }

    // Update is called once per frame
    private void Update()
    {
        // Reset the grid
        if (Input.GetKeyDown(KeyCode.R))
        {
            // Select a random material to use
            m = Materials[Random.Range(0, Materials.Length)];
            SampleIsofield();

            Stopwatch st = new Stopwatch();
            st.Start();

            ExtractIsosurface();
            UnityEngine.Debug.Log("Mesh reset");

            st.Stop();
            UnityEngine.Debug.Log(string.Format("Time to extract isosurface: {0}", st.ElapsedMilliseconds));
        }
        // Clear the grid
        if (Input.GetKeyDown(KeyCode.C))
        {
            ClearGrid();
            ExtractIsosurface();
            UnityEngine.Debug.Log("Grid cleared");
        }
        // Update mesh if outstanding build request
        if (buildRequest)
        {
            ExtractIsosurface();
            buildRequest = false;
        }
    }

    private void OnEnable()
    {
        GridPoint.OnPointValueChange += OnPointValueChange;
    }
    private void OnDisable()
    {
        GridPoint.OnPointValueChange -= OnPointValueChange;
    }
    private void OnPointValueChange(ref GridPoint gp)
    {
        buildRequest = true;
    }

    // Sample isofield to generate terrain
    private void SampleIsofield()
    {
        // "Seed" the noise generation 
        int seed = Random.Range(0, 10000);

        for (int z = 0; z <= GridSize.z; z++)
        {
            for (int y = 0; y <= GridSize.y; y++)
            {
                for (int x = 0; x <= GridSize.x; x++)
                {
                    // Account for grid scale in noise generation
                    float nx = Zoom * (x / GridSize.x);
                    float ny = Zoom * (y / GridSize.y);
                    float nz = Zoom * (z / GridSize.z);

                    // Sample the isofield at the current indices
                    //
                    // Note: This function can be anything as long as it returns some value
                    grid[x, y, z].Value = ny / 4f + Mathf.PerlinNoise(nx + seed, nz + seed) * ny;
                }
            }
        }
        UnityEngine.Debug.Log("Terrain generated");
    }

    // Clear the grid by setting all GridPoint values to 0
    private void ClearGrid()
    {
        for (int z = 0; z <= GridSize.z; z++)
        {
            for (int y = 0; y <= GridSize.y; y++)
            {
                for (int x = 0; x <= GridSize.x; x++)
                {
                    grid[x, y, z].Value = 1f;
                }
            }
        }
    }

    // Initialize the grid
    private void InitializeGrid()
    {
        grid = new GridPoint[(int)GridSize.x + 1, (int)GridSize.y + 1, (int)GridSize.z + 1];
        for (int z = 0; z <= GridSize.z; z++)
        {
            for (int y = 0; y <= GridSize.y; y++)
            {
                for (int x = 0; x <= GridSize.x; x++)
                {
                    // Create a new GridPoint at the current indices
                    GameObject o = new GameObject();
                    o.transform.parent = this.transform;
                    o.AddComponent<BoxCollider>();
                    o.GetComponent<Collider>().isTrigger = true;
                    Rigidbody rb = o.gameObject.AddComponent<Rigidbody>();
                    rb.useGravity = false;
                    GridPoint gp = o.gameObject.AddComponent<GridPoint>();
                    gp.Position = new Vector3(x, y, z);
                    gp.Value = 0f;
                    gp.Size = 0.1f;
                    grid[x, y, z] = gp;
                }
            }
        }
        UnityEngine.Debug.Log("Grid initialized");
    }

    // Use the marching cubes algorithm to extract the isosurface
    private void ExtractIsosurface()
    {
        // Get the gameobject and mesh
        GameObject o = this.gameObject;
        GetMesh(ref o, ref m, true);

        // Clear lists
        vertices.Clear();
        triangles.Clear();
        uv.Clear();

        // March through each cell in the grid
        for (int z = 0; z < GridSize.z; z++)
        {
            for (int y = 0; y < GridSize.y; y++)
            {
                for (int x = 0; x < GridSize.x; x++)
                {
                    // Set the vertices of the cell
                    cell.p[0] = grid[x, y, z + 1];
                    cell.p[1] = grid[x + 1, y, z + 1];
                    cell.p[2] = grid[x + 1, y, z];
                    cell.p[3] = grid[x, y, z];
                    cell.p[4] = grid[x, y + 1, z + 1];
                    cell.p[5] = grid[x + 1, y + 1, z + 1];
                    cell.p[6] = grid[x + 1, y + 1, z];
                    cell.p[7] = grid[x, y + 1, z];
                    MarchingCubes.Triangulate(ref cell, Isolevel);
                    BuildCellMesh(ref cell);
                }
            }
        }

        // Convert the vertex, triangle and uv lists to arrays
        Vector3[] vertexArray = vertices.ToArray();
        int[] triangleArray = triangles.ToArray();
        Vector2[] uvArray = uv.ToArray();

        // Set the grid mesh
        SetMesh(ref o, ref vertexArray, ref triangleArray, ref uvArray);
    }

    // Get a mesh
    public static Mesh GetMesh(ref GameObject go, ref Material material, bool bCollider)
    {
        Mesh m = null;

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr == null)
        {
            mr = go.AddComponent<MeshRenderer>();
        }
        mr.material = material;

        MeshFilter mf = go.GetComponent<MeshFilter>();
        if (mf == null)
        {
            mf = go.AddComponent<MeshFilter>();
        }

        if (Application.isEditor == true)
        {
            if (mf.sharedMesh == null)
            {
                mf.sharedMesh = new Mesh();
            }
            m = mf.sharedMesh;
        }
        else
        {
            if (mf.mesh == null)
            {
                mf.mesh = new Mesh();
            }
            m = mf.mesh;
        }
        m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
        m.name = "Procedural Mesh";

        if (bCollider == true)
        {
            MeshCollider mc = go.GetComponent<MeshCollider>();
            if (mc == null)
            {
                mc = go.AddComponent<MeshCollider>();
            }
            mc.sharedMesh = mf.mesh;
        }

        return m;
    }

    // Set a mesh
    private static Mesh SetMesh(ref GameObject go, ref Vector3[] vertices, ref int[] triangles, ref Vector2[] uv)
    {
        Mesh m = go.GetComponent<MeshFilter>().mesh;

        m.Clear();

        m.vertices = vertices;
        m.triangles = triangles;
        m.uv = uv;

        m.RecalculateBounds();
        m.RecalculateNormals();

        MeshCollider mc = go.GetComponent<MeshCollider>();
        if (mc != null)
        {
            mc.sharedMesh = m;
        }

        return m;
    }

    // Map a 2D texture to a 3D surface
    private static class UV
    {
        public static Vector2 A = new Vector2(0, 1);
        public static Vector2 B = new Vector2(1, 1);
        public static Vector2 C = new Vector2(1, 0);
        public static Vector2 D = new Vector2(0, 0);
    }

    // Build the mesh data for the current cell
    private void BuildCellMesh(ref GridCell cell)
    {
        bool uvAlternate = false;
        for (int i = 0; i < cell.numTriangles; i++)
        {
            // For each triangle append its points to the vertex list
            vertices.Add(cell.triangles[i].p[0]);
            vertices.Add(cell.triangles[i].p[1]);
            vertices.Add(cell.triangles[i].p[2]);
            triangles.Add(vertices.Count - 1);
            triangles.Add(vertices.Count - 2);
            triangles.Add(vertices.Count - 3);

            // Alternate UVs each pass (1 square equals 2 triangles)
            if (uvAlternate == true)
            {
                uv.Add(UV.A);
                uv.Add(UV.C);
                uv.Add(UV.D);
            }
            else
            {
                uv.Add(UV.A);
                uv.Add(UV.B);
                uv.Add(UV.C);
            }
            uvAlternate = !uvAlternate;
        }
    }

    // Visualize grid boundaries
    public void DrawGridBounds()
    {
        float X = GridSize.x;
        float Y = GridSize.y;
        float Z = GridSize.z;

        // Draw each boundary line of the grid
        GridBounds.positionCount = 16;
        GridBounds.SetPosition(0, new Vector3(0, 0, 0));
        GridBounds.SetPosition(1, new Vector3(0, Y, 0));
        GridBounds.SetPosition(2, new Vector3(X, Y, 0));
        GridBounds.SetPosition(3, new Vector3(X, 0, 0));
        GridBounds.SetPosition(4, new Vector3(0, 0, 0));
        GridBounds.SetPosition(5, new Vector3(0, 0, Z));
        GridBounds.SetPosition(6, new Vector3(0, Y, Z));
        GridBounds.SetPosition(7, new Vector3(X, Y, Z));
        GridBounds.SetPosition(8, new Vector3(X, 0, Z));
        GridBounds.SetPosition(9, new Vector3(0, 0, Z));
        GridBounds.SetPosition(10, new Vector3(0, Y, Z));
        GridBounds.SetPosition(11, new Vector3(0, Y, 0));
        GridBounds.SetPosition(12, new Vector3(X, Y, 0));
        GridBounds.SetPosition(13, new Vector3(X, Y, Z));
        GridBounds.SetPosition(14, new Vector3(X, 0, Z));
        GridBounds.SetPosition(15, new Vector3(X, 0, 0));

        UnityEngine.Debug.Log("Boundaries drawn");
    }
}
