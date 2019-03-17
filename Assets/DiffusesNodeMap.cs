using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DiffusesNodeMap : MonoBehaviour
{
    [SerializeField] PlayerLogic _playerReference;
    [SerializeField] DiffuseNode _topPValueNode;
    [SerializeField] float _tickRate = 0.1f;
    private float tickTimer = 0;
    [SerializeField] Transform _plane;
    [SerializeField] float waveGeneratorMaxDelay = 6f;
    [Range(25.0f, 10000.0f)]
    [SerializeField] float waveGeneratorPValueMax = 100f;
    [SerializeField] float _playerCent = 5f;
    [SerializeField] float _playerCent2 = 10.0f;
    [SerializeField] List<DiffuseNodeExternal> _objects;
    [SerializeField] bool _debugDiffuseMap = false;
    [SerializeField] bool showNodeProperties = false;
    [SerializeField] bool showWalls = false;
    [Range(0.0f, 4.0f)]
    [SerializeField] float diffuseFactor = 1f;
    [Range(0.0f, 200.0f)]
    [SerializeField] float thresholdDepth = 0.15f;
    [Range(0.0f, 200.0f)]
    [SerializeField] float threshold = 0.15f;
    MeshRenderer planeMeshRenderer;
    Material planeMeshMaterial;
    [SerializeField] Transform[] bakeObjects;

    struct TransformNodeSettings
    {
        public float PValue;
    }

    [System.Serializable]
    public class DiffuseNodeExternal : System.Object
    {
        public Transform transform;
        public string name;
        public float p;
        public float lambda;
        public Vector2Int index;
        public Color color;
        public bool showNeighbours;
        public bool showHighestNode;
    }

    public struct DiffuseNode
    {
        public float p;
        public Vector3 position;
        public bool wall;
        public Vector2Int index;
        public float lambda;
        public float diffuseFactor;
    };

    public struct ArrayInfo
    {
        public int ARRAY_WIDTH;
        public int ARRAY_HALF_WIDTH;
        public int ARRAY_MAX;
        public int ARRAY_MIN;
        public int ACTUAL_ARRAY_WIDTH;
        public int VOXEL_SIZE;
        public float VOXEL_HALF_WIDTH;
    }

    ArrayInfo arrayInfo;

    DiffuseNode[,] diffuseNodeArray;

    DiffuseNode[,] diffuseNodeArrayBackup;

    [SerializeField]
    List<DiffuseNodeExternal> diffuseNodeArrayExternal;

    DiffuseNode playerNode, playerNode2;
    DiffuseNode tnode;
    Vector2Int playerPos, playerPos2;
    Vector2Int lastPlayerPos;
    [SerializeField] Vector2Int smthPos;

    public float maxPValue = 10f;
    float ratio;
    float planeScale;
    Vector3 planePos;
    int[,] Map;
    Texture2D planeTexture;
    float VOXEL_HALF_WIDTH = 0.5f;

    void Start()
    {
        planeMeshRenderer = _plane.transform.GetComponent<MeshRenderer>();
        planeMeshRenderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        planeMeshRenderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        planeMeshRenderer.material.SetInt("_ZWrite", 0);
        planeMeshRenderer.material.DisableKeyword("_ALPHATEST_ON");
        planeMeshRenderer.material.DisableKeyword("_ALPHABLEND_ON");
        planeMeshRenderer.material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        planeMeshRenderer.material.renderQueue = 3000;
        planeMeshMaterial = planeMeshRenderer.material;

        arrayInfo.ARRAY_WIDTH = (int)_plane.localScale.x * 10;
        arrayInfo.ARRAY_MIN = 0;
        arrayInfo.ARRAY_MAX = arrayInfo.ARRAY_WIDTH;
        arrayInfo.ARRAY_HALF_WIDTH = arrayInfo.ARRAY_WIDTH / 2;
        arrayInfo.ACTUAL_ARRAY_WIDTH = arrayInfo.ARRAY_WIDTH; //map size + boundaries
        arrayInfo.VOXEL_SIZE = 1;
        arrayInfo.VOXEL_HALF_WIDTH = 0.5f;

        planeTexture = new Texture2D(arrayInfo.ARRAY_WIDTH, arrayInfo.ARRAY_WIDTH, TextureFormat.ARGB32, false);
        diffuseNodeArray = new DiffuseNode[arrayInfo.ACTUAL_ARRAY_WIDTH, arrayInfo.ACTUAL_ARRAY_WIDTH];
        //ConvertMapTo2DArray(Map, ref diffuseNodeArray);
        GenerateMapFromNavMesh(ref diffuseNodeArray);

        planePos = _plane.transform.position;
        _plane.transform.localScale = new Vector3((float)arrayInfo.ARRAY_WIDTH / 10f, 1, (float)arrayInfo.ARRAY_WIDTH / 10f);
        //_plane.transform.position = new Vector3(
        //    _plane.transform.position.x + arrayInfo.ARRAY_MAX * 0.5f,
        //    _plane.transform.position.y,
        //    _plane.transform.position.z + arrayInfo.ARRAY_MAX * 0.5f);
    }

    public void RegisterObject(Transform t)
    {
        DiffuseNodeExternal node = new DiffuseNodeExternal();
        node.color = new Color(Random.Range(0.2f, 1f), Random.Range(0.2f, 1f), Random.Range(0.2f, 1f), 0.5f);
        node.index = ConvertPositionToIndex(t.position);
        node.name = t.name;
        node.p = 0f;
        node.showNeighbours = false;
        node.showHighestNode = false;
        node.transform = t;
        _objects.Add(node);
    }

    Vector2Int ClampIn2DArray(float x, float y)
    {
        Vector2Int result = new Vector2Int();
        result.x = Mathf.Clamp(Mathf.RoundToInt(x - arrayInfo.VOXEL_HALF_WIDTH), -arrayInfo.ARRAY_HALF_WIDTH, arrayInfo.ARRAY_HALF_WIDTH - 1) + arrayInfo.ARRAY_HALF_WIDTH;
        result.y = Mathf.Clamp(Mathf.RoundToInt(y - arrayInfo.VOXEL_HALF_WIDTH), -arrayInfo.ARRAY_HALF_WIDTH, arrayInfo.ARRAY_HALF_WIDTH - 1) + arrayInfo.ARRAY_HALF_WIDTH;
        return result;
    }

    public Vector3 getHighestNodeDirection(Vector2Int index)
    {
        DiffuseNode topNode = getHighestNeighbour(index.y, index.x);
        Vector3 result;
        result = Vector3.Normalize(topNode.position - diffuseNodeArray[index.y, index.x].position);
        return result;
    }

    public Vector3 getHighestNodeDirection(Vector3 position)
    {
        Vector2Int index = ClampIn2DArray(position.x, position.z);
        DiffuseNode topNode = getHighestNeighbour(index.y, index.x);
        Vector3 result;
        result = Vector3.Normalize(topNode.position - diffuseNodeArray[index.y, index.x].position);
        return result;
    }

    public Vector3 getHighestNodePosition(Vector3 position)
    {
        Vector2Int index = ClampIn2DArray(position.x, position.z);
        DiffuseNode topNode = getHighestNeighbour(index.y, index.x);
        Vector3 result = topNode.position;
        return result;
    }

    public Vector3 getHighestNodePosition(int x, int z)
    {
        DiffuseNode topNode = getHighestNeighbour(z, x);
        Vector3 result = topNode.position;
        return result;
    }

    public Vector2Int ConvertPositionToIndex(Vector3 input)
    {
        Vector2Int result = new Vector2Int();
        result.x = Mathf.Clamp(Mathf.RoundToInt(input.x - arrayInfo.VOXEL_HALF_WIDTH), -arrayInfo.ARRAY_HALF_WIDTH, arrayInfo.ARRAY_HALF_WIDTH - 1) + arrayInfo.ARRAY_HALF_WIDTH;
        result.y = Mathf.Clamp(Mathf.RoundToInt(input.z - arrayInfo.VOXEL_HALF_WIDTH), -arrayInfo.ARRAY_HALF_WIDTH, arrayInfo.ARRAY_HALF_WIDTH - 1) + arrayInfo.ARRAY_HALF_WIDTH;
        return result;
    }

    public Vector2Int getClosestNodeToGrid(Vector3 position)
    {
        Vector2Int index = new Vector2Int();
        float min_distance = 5.5f;
        float distance = 0;
        Vector3 center;
        Vector3 other;
        for (int z = arrayInfo.ARRAY_MIN; z < arrayInfo.ARRAY_MAX; z++)
        {
            for (int x = arrayInfo.ARRAY_MIN; x < arrayInfo.ARRAY_MAX; x++)
            {
                center = new Vector3(position.x, 0, position.z);
                other = new Vector3(diffuseNodeArray[z, x].position.x, 0, diffuseNodeArray[z, x].position.z);
                distance = Vector3.Distance(center, other);
                if (distance < min_distance)
                {
                    min_distance = distance;
                    index = new Vector2Int(x, z);
                }
            }
        }
        Debug.DrawLine(position, diffuseNodeArray[index.y, index.x].position);
        return index;
    }

    void GenerateMapFromNavMesh(ref DiffuseNode[,] array)
    {
        int[] polygons;
        Vector3[] vertices;
        NavMeshTriangulation navMesh = NavMesh.CalculateTriangulation();
        vertices = navMesh.vertices;
        polygons = navMesh.indices;

        array = new DiffuseNode[arrayInfo.ACTUAL_ARRAY_WIDTH, arrayInfo.ACTUAL_ARRAY_WIDTH];
        for (int z = arrayInfo.ARRAY_MIN; z < arrayInfo.ARRAY_MAX; z++)
        {
            for (int x = arrayInfo.ARRAY_MIN; x < arrayInfo.ARRAY_MAX; x++)
            {
                DiffuseNode t = new DiffuseNode();
                NavMeshHit hit;
                VOXEL_HALF_WIDTH = 0.5f;
                if (NavMesh.SamplePosition(new Vector3(planePos.x + x - arrayInfo.ARRAY_HALF_WIDTH + VOXEL_HALF_WIDTH, 1.0f, planePos.z + z - arrayInfo.ARRAY_HALF_WIDTH + VOXEL_HALF_WIDTH), out hit, 1.0f, NavMesh.AllAreas))
                {
                    t.wall = false;
                    t.lambda = 1.0f;
                    t.p = 1.0f;
                    t.position = new Vector3(planePos.x - arrayInfo.ARRAY_HALF_WIDTH + x + VOXEL_HALF_WIDTH, hit.position.y, planePos.z - arrayInfo.ARRAY_HALF_WIDTH + z + VOXEL_HALF_WIDTH);
                    diffuseNodeArray[z, x] = t;
                }
                else
                {
                    t.wall = false;
                    t.lambda = 1.0f;
                    t.p = 1.0f;
                    t.position = new Vector3(planePos.x - arrayInfo.ARRAY_HALF_WIDTH + x + VOXEL_HALF_WIDTH, planePos.y, planePos.z - arrayInfo.ARRAY_HALF_WIDTH + z + VOXEL_HALF_WIDTH);
                    diffuseNodeArray[z, x] = t;
                }
            }
        }
    }

    public void getNeighbours(out List<DiffuseNode> result, int z, int x)
    {
        result = new List<DiffuseNode>();
        DiffuseNode node;
        if (x - 1 >= arrayInfo.ARRAY_MIN)
        {
            if (diffuseNodeArray[z, x - 1].wall != true)
            {
                node = diffuseNodeArray[z, x - 1];
                node.index = new Vector2Int(x - 1, z);
                result.Add(node);
            }
        }
        if (x + 1 < arrayInfo.ARRAY_MAX)
        {
            if (diffuseNodeArray[z, x + 1].wall != true)
            {
                node = diffuseNodeArray[z, x + 1];
                node.index = new Vector2Int(x + 1, z);
                result.Add(node);
            }
        }
        if (z - 1 >= arrayInfo.ARRAY_MIN)
        {
            if (diffuseNodeArray[z - 1, x].wall != true)
            {
                node = diffuseNodeArray[z - 1, x];
                node.index = new Vector2Int(x, z - 1);
                result.Add(node);
            }
        }
        if (z + 1 < arrayInfo.ARRAY_MAX)
        {
            if (diffuseNodeArray[z + 1, x].wall != true)
            {
                node = diffuseNodeArray[z + 1, x];
                node.index = new Vector2Int(x, z + 1);
                result.Add(node);
            }
        }
        //if (result.Count == 0) Debug.LogError("Zero neighbours found at index: " + z + ", " + x);
    }

    public DiffuseNode getHighestNeighbour(int z, int x)
    {
        DiffuseNode result = new DiffuseNode();
        DiffuseNode centerNode = diffuseNodeArray[z, x];
        List<DiffuseNode> n;
        getNeighbours(out n, z, x);

        float top_p = 0;
        Vector2Int top_index = new Vector2Int();

        for (int i = 0; i < n.Count; i++)
        {
            if (n[i].p > centerNode.p && n[i].p > top_p)
            {
                top_index = n[i].index;
                top_p = n[i].p;
            }
        }
        result = diffuseNodeArray[top_index.y, top_index.x];
        return result;
    }

    public bool bake = false;

    void FixedUpdate()
    {
        tickTimer += Time.deltaTime;

        //foreach (Transform t in bakeObjects)
        //{
        //    Vector2Int index = new Vector2Int();
        //    Vector2Int scale = new Vector2Int((int)transform.localScale.x, (int)transform.localScale.y);
        //    for (int y = 0; y < scale.y; y++)
        //    {
        //        for (int x = 0; x < scale.x; x++)
        //        {
        //            index = ClampIn2DArray(t.transform.position.x, t.transform.position.z);
        //            diffuseNodeArray[index.y, index.x].wall = true;
        //        }
        //    }

        //}

        for (int j = 0; j < _objects.Count; j++)
        {
            DiffuseNodeExternal t = _objects[j];
            if (t.transform)
            {
                DiffuseNodeExternal n = new DiffuseNodeExternal();
                n = t;
                n.index = ClampIn2DArray(t.transform.position.x, t.transform.position.z);
                _objects[j] = n;
                diffuseNodeArray[_objects[j].index.y, _objects[j].index.x].p = _objects[j].p;
            }
        }

        for (int z = arrayInfo.ARRAY_MIN; z < arrayInfo.ARRAY_MAX; z++)
        {
            for (int x = arrayInfo.ARRAY_MIN; x < arrayInfo.ARRAY_MAX; x++)
            {
                DiffuseNode t = diffuseNodeArray[z, x];
                if (t.wall) continue;

                DiffuseNode result = new DiffuseNode();
                result = t;

                List<DiffuseNode> n;
                getNeighbours(out n, z, x);

                //float psum = diffuseFactor  * (n.left.p + n.right.p + n.up.p + n.down.p / (((n.left.p - t.p)) + ((n.right.p - t.p)) + ((n.up.p - t.p)) + ((n.down.p - t.p))));
                if (t.p > lastMaxPValue) lastMaxPValue = t.p;

                float psum = 0;
                for (int j = 0; j < n.Count; j++)
                {
                    psum += diffuseNodeArray[n[j].index.y, n[j].index.x].p;
                    //psumDelta += diffuseNodeArray[neighbour_indices[j]].p - diffuseNodeArray[i].p;
                }
                result.p = psum / (n.Count * diffuseFactor);

                float alpha = 0.25f;
                Color c = new Color(1f - (t.p / maxPValue), (t.p / maxPValue), 0, alpha);
                planeTexture.SetPixel(arrayInfo.ARRAY_WIDTH - (x + 1), arrayInfo.ARRAY_WIDTH - (z + 1), c);

                planeTexture.SetPixel(0, 0, Color.blue);
                planeTexture.SetPixel(0, arrayInfo.ARRAY_WIDTH, Color.blue);
                planeTexture.SetPixel(arrayInfo.ARRAY_WIDTH, arrayInfo.ARRAY_WIDTH, Color.blue);
                planeTexture.SetPixel(arrayInfo.ARRAY_WIDTH, 0, Color.blue);

                diffuseNodeArray[z, x] = result;
            }
        }
        planeTexture.Apply();
        planeMeshMaterial.mainTexture = planeTexture;

        //foreach (Transform t in bakeObjects)
        //{
        //    t.localPosition = GetNearestPointOnGrid(t.localPosition, 1.0f);
        //    //t.localScale = GetNearestPointOnGrid(t.localScale * 1.25f);
        //    Vector2Int index = new Vector2Int();
        //    Vector2Int scale = new Vector2Int((int)t.transform.localScale.x, (int)t.transform.localScale.z);
        //    bool enable = false;
        //    apply:
        //    for (float y = -scale.y * 0.5f + 0.5f; y < scale.y * 0.5f + 0.5f; y++)
        //    {
        //        for (float x = -scale.x * 0.5f + 0.5f; x < scale.x * 0.5f + 0.5f; x++)
        //        {
        //            if (bake)
        //            {
        //                bake = false;
        //                enable = true;
        //                goto apply;
        //            }
        //            if (enable)
        //            {
        //                index = ClampIn2DArray(t.transform.position.x + x, t.transform.position.z + y);
        //                diffuseNodeArray[index.y + 1, index.x + 1].wall = true;
        //            }
        //        }
        //    }
        //}
    }
    public Vector3 GetNearestPointOnGrid(Vector3 position, float size = 1.0f)
    {
        position -= transform.position;

        int xCount = Mathf.RoundToInt(position.x / size);
        int yCount = Mathf.RoundToInt(position.y / size);
        int zCount = Mathf.RoundToInt(position.z / size);

        Vector3 result = new Vector3(
            (float)xCount * size,
            position.y,
            (float)zCount * size);

        result += transform.position;

        return result;
    }

    float lastMaxPValue = 0;
    void OnDrawGizmos()
    {
        if (diffuseNodeArray != null && _debugDiffuseMap)
        {
            for (int x = 0; x < arrayInfo.ARRAY_MAX; x++)
            {
                for (int z = 0; z < arrayInfo.ARRAY_MAX; z++)
                {
                    DiffuseNode node = diffuseNodeArray[z, x];

                    if (node.wall && showWalls)
                    {
                        Gizmos.color = Color.grey;
                        Gizmos.DrawCube(node.position + Vector3.up * 0.5f, Vector3.one);
                    }
                    else
                    {
                        Color c = new Color(1f - (node.p / lastMaxPValue), (node.p / lastMaxPValue), 0, (node.p / lastMaxPValue));
                        Gizmos.color = c;
                        float scaleY = node.p / lastMaxPValue;
                        Gizmos.DrawCube(node.position + Vector3.up * scaleY * maxPValue * 0.5f, new Vector3(1f, maxPValue * scaleY, 1f));
                    }
                    
                }
            }
        }
        if(_objects.Count > 0 && diffuseNodeArray != null)
        {
            foreach (DiffuseNodeExternal t in _objects)
            {
                Gizmos.color = t.color;
                DiffuseNode n = diffuseNodeArray[t.index.y, t.index.x];
                float scaleY = n.p / lastMaxPValue;
                Gizmos.DrawCube(n.position + Vector3.up * scaleY * maxPValue * 0.5f, new Vector3(1f, maxPValue * scaleY, 1f));
                if (t.showNeighbours)
                {
                    List<DiffuseNode> c;
                    getNeighbours(out c, t.index.y, t.index.x);
                    Gizmos.color = Color.yellow;
                    for (int j = 0; j < c.Count; j++)
                    {
                        scaleY = c[j].p / lastMaxPValue;
                        Gizmos.DrawCube(c[j].position + Vector3.up * scaleY * maxPValue * 0.5f, new Vector3(1f, maxPValue * scaleY, 1f));
                    }
                }
                if (t.showHighestNode)
                {
                    DiffuseNode tn = getHighestNeighbour(t.index.y, t.index.x);
                    scaleY = tn.p / lastMaxPValue;
                    Gizmos.color = Color.white;
                    Gizmos.DrawCube(tn.position + Vector3.up * scaleY * maxPValue * 0.5f, new Vector3(1f, maxPValue * scaleY, 1f));
                }
            }
        }
    }
}