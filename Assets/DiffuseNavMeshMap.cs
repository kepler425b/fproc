using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DiffuseNavMeshMap : MonoBehaviour
{
    [SerializeField] PlayerLogic _playerReference;
    [SerializeField] DiffuseNode _topPValueNode;
    [SerializeField] float _tickRate = 0.1f;
    private float tickTimer = 0;
    [SerializeField] Transform _plane;
    [Range(4, 32)]
    [SerializeField] int fontSize = 4;

    [Range(0, 800)]
    [SerializeField] int index = 0;


    [SerializeField] float _playerCent = 5f;
    [SerializeField] float _playerCent2 = 10.0f;
    [SerializeField] DiffuseNodeExternal[] _objects;
    [SerializeField] bool _debugDiffuseMap = false;
    [SerializeField] bool showNodeProperties = false;
    [Range(0.0f, 200.0f)]
    [SerializeField] float diffuseFactor = 10f;
    [Range(0.0f, 200.0f)]
    [SerializeField] float thresholdDepth = 0.15f;
    [Range(0.0f, 200.0f)]
    [SerializeField] float threshold = 0.15f;
    MeshRenderer planeMeshRenderer;
    Material planeMeshMaterial;
    [SerializeField] Transform[] bakeObjects;

    public class Debug2String
    {
        public Vector3 pos;
        public string text;
        public Color? color;
        public float eraseTime;
    }

    public List<Debug2String> Strings = new List<Debug2String>();


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
        public int index;
        public Color color;
        public Vector3 position;
        public Vector3 nodePosition;
    }

    public struct DiffuseNode
    {
        public float p;
        public Vector3 position;
        public bool wall;
        public float lambda;
        public float diffuseFactor;
        public int arrayIndex;
        public int polygonIndex;
    };

    public struct DiffuseNodeNeighbours
    {
        public DiffuseNode[] nodes;
    };

    public struct ArrayInfo
    {
        public int ARRAY_WIDTH;
        public int ARRAY_HALF_WIDTH;
        public int ARRAY_MAX;
        public int ARRAY_MIN;
        public int ACTUAL_ARRAY_WIDTH;
    }

    ArrayInfo arrayInfo;

    DiffuseNode[] diffuseNodeArray;

    [SerializeField]
    DiffuseNodeExternal[] diffuseNodeArrayExternal;

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
    float gizmoCubeOffsetToCenter = 0.5f;

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

        diffuseNodeArrayExternal = new DiffuseNodeExternal[1];
        DiffuseNodeExternal n = new DiffuseNodeExternal();
        for (int j = 0; j < diffuseNodeArrayExternal.Length; j++)
        {
            DiffuseNodeExternal t = _objects[j];
            n.transform = t.transform;
            n.position = t.transform.position;
            n.name = t.transform.name;
            n.index = ClampInArray(t.transform.position);
            n.p = t.p;
            n.color = t.color;
            diffuseNodeArrayExternal[j] = n;
        }


        arrayInfo.ACTUAL_ARRAY_WIDTH = arrayInfo.ARRAY_WIDTH + 2; //map size + boundaries

        planeTexture = new Texture2D(arrayInfo.ARRAY_WIDTH, arrayInfo.ARRAY_WIDTH, TextureFormat.ARGB32, false);
        //ConvertMapTo2DArray(Map, ref diffuseNodeArray);
        GenerateMapFromNavMesh(ref diffuseNodeArray);

        planePos = _plane.transform.position;
        _plane.transform.localScale = new Vector3((float)arrayInfo.ARRAY_WIDTH / 10f, 1, (float)arrayInfo.ARRAY_WIDTH / 10f);
        //_plane.transform.position = new Vector3(
        //    _plane.transform.position.x + arrayInfo.ARRAY_MAX * 0.5f,
        //    _plane.transform.position.y,
        //    _plane.transform.position.z + arrayInfo.ARRAY_MAX * 0.5f);
    }

    public static void DrawText(Vector3 pos, string text, Color? color = null)
    {
        instance.Strings.Add(new Debug2Mono.Debug2String() { text = text, color = color, pos = pos, eraseTime = Time.time + 0.01f, });
        List<Debug2Mono.Debug2String> toBeRemoved = new List<Debug2Mono.Debug2String>();
        foreach (var item in instance.Strings)
        {
            if (item.eraseTime <= Time.time)
                toBeRemoved.Add(item);
        }
        foreach (var rem in toBeRemoved)
            instance.Strings.Remove(rem);
    }

    List<int> neighbours = new List<int>();

    NavMeshTriangulation navMesh;
    int[] polygons;
    Vector3[] vertices;

    int size;
    int arrayColSize, arrayRowSize;

    void GenerateMapFromNavMesh(ref DiffuseNode[] array)
    {
        navMesh = NavMesh.CalculateTriangulation();
        vertices = navMesh.vertices;
        polygons = navMesh.indices;
        size = polygons.Length;
        array = new DiffuseNode[size];
        arrayColSize = 20;
        arrayRowSize = 20;

        arrayInfo.ARRAY_MAX = size;
        arrayInfo.ARRAY_MIN = 0;
        arrayInfo.ARRAY_HALF_WIDTH = arrayInfo.ARRAY_MAX / 2;

        for (int i = arrayInfo.ARRAY_MIN; i < arrayInfo.ARRAY_MAX; i++)
        {
            DiffuseNode t = new DiffuseNode();
            t.wall = false;
            t.lambda = 1.0f;
            t.p = 1.0f;
            t.position = vertices[polygons[i]];
            t.arrayIndex = ClampInArray(t.position);
            t.polygonIndex = polygons[i];
            array[t.arrayIndex] = t;
        }
    }

    int ClampInArray(Vector3 input)
    {
        int result;
        Vector2Int temp = new Vector2Int();
        temp.x = arrayRowSize + (int)Mathf.Clamp(input.x, -arrayRowSize, arrayRowSize);
        temp.y = arrayColSize + (int)Mathf.Clamp(input.z, -arrayColSize, arrayColSize);
        result = arrayRowSize * temp.y + temp.x;
        return result;
    }

    public void getNeighbours(out DiffuseNodeNeighbours result, Vector3 position)
    {
        result.nodes = new DiffuseNode[3];
        for (int i = 0; i < size / 3; i++)
        {
            int vert = i * 3;
            index = ClampInArray(position);
            if (polygons[vert] == index || polygons[vert + 1] == index || polygons[vert + 2] == index)
            {
                if (polygons[vert] == index)
                {
                    result.nodes[0].arrayIndex = index;
                }
                if (polygons[vert + 1] == index)
                {
                    result.nodes[1].arrayIndex = index;
                }
                if (polygons[vert + 2] == index)
                {
                    result.nodes[2].arrayIndex = index;
                }
            }
        }
    }


    public bool bake = false;
    DiffuseNodeNeighbours nb = new DiffuseNodeNeighbours();
    DiffuseNodeExternal n = new DiffuseNodeExternal();
    void FixedUpdate()
    {
        tickTimer += Time.deltaTime;

        if(_objects.Length > 0)
        {
            for(int j = 0; j < _objects.Length; j++)
            {
                DiffuseNodeExternal t = _objects[j];
                if (t.transform)
                {
                    n.transform = t.transform;
                    n.position = t.transform.position;
                    n.name = t.transform.name;
                    n.index = ClampInArray(t.transform.position);
                    n.nodePosition = diffuseNodeArray[t.index].position;
                    n.p = t.p;
                    n.color = t.color;
                    //diffuseNodeArrayExternal[j] = t;
                    _objects[j] = n;
                }
                else
                {
                }
            }
        }
        if (_objects[0].transform)
            getNeighbours(out nb, diffuseNodeArray[_objects[0].index].position);
    }

    void OnDrawGizmos()
    {
        if (diffuseNodeArray != null && _debugDiffuseMap)
        {
            for (int i = 0; i < arrayInfo.ARRAY_MAX; i++)
            {
                if (diffuseNodeArray[i].wall)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawWireCube(diffuseNodeArray[i].position + Vector3.up * 0.5f, Vector3.one);
                }
                else if (true)
                {
                    DiffuseNode node = diffuseNodeArray[i];
                    Color c = new Color(1 - (node.p / maxPValue * diffuseFactor), (node.p / maxPValue * diffuseFactor), 0, 1.0f);
                    Gizmos.color = c;
                    Gizmos.DrawCube(node.position, new Vector3(0.25f, 0.25f, 0.25f));
                    //DrawText(node.position + Vector3.up, node.arrayIndex.ToString());
                    if(nb.nodes != null)
                    {
                        if (nb.nodes.Length > 0)
                        {
                            for (int j = 0; j < nb.nodes.Length; j++)
                            {
                                Gizmos.color = Color.red;
                                Gizmos.DrawCube(nb.nodes[j].position, new Vector3(0.5f, 0.5f, 0.5f));
                            }
                        }
                    }
                }
            }
            if (_objects.Length > 0)
            {
                for (int j = 0; j < _objects.Length; j++)
                {
                    if (_objects[j].transform)
                    {
                        Gizmos.color = _objects[j].color;
                        Gizmos.DrawCube(diffuseNodeArray[_objects[j].index].position, new Vector3(0.75f, 0.75f, 0.75f));
                    }
                }
            }
        }
        foreach (var stringpair in Strings)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = fontSize;
            Color color = stringpair.color.HasValue ? stringpair.color.Value : Color.green;
            style.normal.textColor = color;
#if UNITY_EDITOR
            UnityEditor.Handles.color = color;
            UnityEditor.Handles.Label(stringpair.pos, stringpair.text, style);
#endif
        }
    }

    private static Debug2Mono m_instance;
    public static Debug2Mono instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = GameObject.FindObjectOfType<Debug2Mono>();
                if (m_instance == null)
                {
                    var go = new GameObject("DeleteMeLater");
                    m_instance = go.AddComponent<Debug2Mono>();
                }
            }
            return m_instance;
        }
    }
}
