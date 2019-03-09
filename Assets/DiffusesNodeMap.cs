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
    [SerializeField] DiffuseNodeExternal[] _objects;
    [SerializeField] bool _debugDiffuseMap = false;
    [SerializeField] bool showNodeProperties = false;
    [Range(0.0f, 200.0f)]
    [SerializeField] float diffuseFactor =  10f;
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
        public Vector2Int index;
        public Color color;
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

    public struct DiffuseNodeNeighbours
    {
        public DiffuseNode left, right, up, down;
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
        Map = new int[,] {
            { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
            { 1, 0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1}
        };

        diffuseNodeArrayExternal = new List<DiffuseNodeExternal>();
        foreach (DiffuseNodeExternal t in _objects)
        {
            DiffuseNodeExternal n = new DiffuseNodeExternal();
            Vector2Int vec = new Vector2Int((int)t.transform.position.z, (int)t.transform.position.x);
            n.transform = t.transform;
            n.name = t.transform.name;
            n.index = ClampIn2DArray(vec);
            n.p = t.p;
            n.color = t.color;
            diffuseNodeArrayExternal.Add(n);
        }

        arrayInfo.ARRAY_WIDTH = (int)_plane.localScale.x * 10;
        arrayInfo.ARRAY_MIN = 1;
        arrayInfo.ARRAY_MAX = arrayInfo.ARRAY_WIDTH;
        arrayInfo.ARRAY_HALF_WIDTH = arrayInfo.ARRAY_WIDTH / 2;
        arrayInfo.ACTUAL_ARRAY_WIDTH = arrayInfo.ARRAY_WIDTH + 2; //map size + boundaries

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

    private void OnEnable()
    {
        
    }

    private void OnDisable()
    {

    }

    public static void DrawText(Vector3 pos, string text, Color? color = null)
    {
        instance.Strings.Add(new Debug2Mono.Debug2String() { text = text, color = color, pos = pos, eraseTime = Time.time + 0.2f, });
        List<Debug2Mono.Debug2String> toBeRemoved = new List<Debug2Mono.Debug2String>();
        foreach (var item in instance.Strings)
        {
            if (item.eraseTime <= Time.time)
                toBeRemoved.Add(item);
        }
        foreach (var rem in toBeRemoved)
            instance.Strings.Remove(rem);
    }

    void MoveIn2DArray(ref Vector2Int input)
    {
        if (Input.GetKey(KeyCode.RightArrow)) input.x += 1;
        if (Input.GetKey(KeyCode.LeftArrow))  input.x -= 1;
        if (Input.GetKey(KeyCode.DownArrow))  input.y -= 1;
        if (Input.GetKey(KeyCode.UpArrow))    input.y += 1;
        ClampIn2DArray(ref input);
    }

    void MapVectorIn2DArray(Vector3 input, float PValue)
    {
        Vector2Int t = new Vector2Int((int)input.x, (int)input.z);
        ClampIn2DArray(ref t);
        diffuseNodeArray[t.y, t.x].p = PValue;
    }

    void ClampIn2DArray(ref Vector2Int input)
    {
        input.x = Mathf.Clamp(input.x, -arrayInfo.ARRAY_HALF_WIDTH, arrayInfo.ARRAY_HALF_WIDTH - 2) + arrayInfo.ARRAY_HALF_WIDTH;
        input.y = Mathf.Clamp(input.y, -arrayInfo.ARRAY_HALF_WIDTH, arrayInfo.ARRAY_HALF_WIDTH - 2) + arrayInfo.ARRAY_HALF_WIDTH;
    }

    Vector2Int ClampIn2DArray(Vector2Int input)
    {
        Vector2Int result = new Vector2Int();
        result.x = Mathf.Clamp(input.x, -arrayInfo.ARRAY_HALF_WIDTH, arrayInfo.ARRAY_HALF_WIDTH - 2) + arrayInfo.ARRAY_HALF_WIDTH;
        result.y = Mathf.Clamp(input.y, -arrayInfo.ARRAY_HALF_WIDTH, arrayInfo.ARRAY_HALF_WIDTH - 2) + arrayInfo.ARRAY_HALF_WIDTH;
        return result;
    }

    Vector2Int ClampIn2DArray(float x, float y)
    {
        Vector2Int result = new Vector2Int();
        result.x = Mathf.Clamp(Mathf.RoundToInt(x), -arrayInfo.ARRAY_HALF_WIDTH, arrayInfo.ARRAY_HALF_WIDTH - 2) + arrayInfo.ARRAY_HALF_WIDTH;
        result.y = Mathf.Clamp(Mathf.RoundToInt(y), -arrayInfo.ARRAY_HALF_WIDTH, arrayInfo.ARRAY_HALF_WIDTH - 2) + arrayInfo.ARRAY_HALF_WIDTH;
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

    public Vector3 ClampIn2DArray(Vector3 input)
    {
        Vector3 result = new Vector3();
        result.x = Mathf.Clamp(input.x, planePos.x + -arrayInfo.ARRAY_HALF_WIDTH + 2, planePos.x + arrayInfo.ARRAY_HALF_WIDTH - 2);
        result.y = input.y;
        result.z = Mathf.Clamp(input.z, planePos.x + -arrayInfo.ARRAY_HALF_WIDTH + 2, planePos.x + arrayInfo.ARRAY_HALF_WIDTH - 2);
        return result;
    }

    void ConvertMapTo2DArray(int[,] map, ref DiffuseNode[,] array)
    {
        for(int x = 0; x < map.GetLength(0); x++)
        {
            for (int z = 0; z < map.GetLength(0); z++)
            {
                //if (z < arrayInfo.ARRAY_MIN || x < arrayInfo.ARRAY_MIN) continue;
                //if (z > arrayInfo.ARRAY_MAX || x > arrayInfo.ARRAY_MAX) break;
                DiffuseNode t = new DiffuseNode();
                t.wall = map[z, x] == 0 ? false : true;
                t.lambda = 1.0f;// map[z, x] == 0 ? 1.0f : 0.0f;
                t.p = map[z, x] == 0 ? 0.0f : 1.0f;
                t.position = new Vector3(planePos.x - arrayInfo.ARRAY_HALF_WIDTH + x + gizmoCubeOffsetToCenter, planePos.y, planePos.z - arrayInfo.ARRAY_HALF_WIDTH + z + gizmoCubeOffsetToCenter);
                diffuseNodeArray[z+1, x+1] = t;
            }
        }
    }

    void GenerateMapFromNavMesh(ref DiffuseNode[,] array)
    {
        array = new DiffuseNode[arrayInfo.ACTUAL_ARRAY_WIDTH, arrayInfo.ACTUAL_ARRAY_WIDTH];
        for (int x = arrayInfo.ARRAY_MIN; x < arrayInfo.ARRAY_WIDTH; x++)
        {
            for (int z = arrayInfo.ARRAY_MIN; z < arrayInfo.ARRAY_WIDTH; z++)
            {
                DiffuseNode t = new DiffuseNode();
                NavMeshHit hit;
                gizmoCubeOffsetToCenter = 0f;
                if (NavMesh.SamplePosition(new Vector3(planePos.x + x -arrayInfo.ARRAY_HALF_WIDTH + gizmoCubeOffsetToCenter, 1.0f, planePos.z + z - arrayInfo.ARRAY_HALF_WIDTH + gizmoCubeOffsetToCenter), out hit, 1.0f, NavMesh.AllAreas))
                {
                    t.wall = false;
                    t.lambda = 1.0f;
                    t.p = 1.0f;
                    t.position = new Vector3(hit.position.x, planePos.y, hit.position.z);
                    diffuseNodeArray[z + 1, x + 1] = t;
                }
                else
                {
                    t.wall = true;
                    t.lambda = 1.0f;
                    t.p = 0.0f;
                    t.position = new Vector3(planePos.x - arrayInfo.ARRAY_HALF_WIDTH + x + gizmoCubeOffsetToCenter, planePos.y, planePos.z - arrayInfo.ARRAY_HALF_WIDTH + z + gizmoCubeOffsetToCenter);
                    diffuseNodeArray[z + 1, x + 1] = t;
                }
            }
        }
    }
    bool IEGenerateRandomWavesReturn = true;
    public IEnumerator IEGenerateRandomWaves()
    {
        float t = 0;
        DiffuseNode n;
        int x = Random.Range(arrayInfo.ARRAY_MIN, arrayInfo.ARRAY_MAX);
        int z = Random.Range(arrayInfo.ARRAY_MIN, arrayInfo.ARRAY_MAX);
        float p = Random.Range(25f, waveGeneratorPValueMax);
        float duration = Random.Range(0.5f, waveGeneratorMaxDelay);
        n = diffuseNodeArray[z, x];
        IEGenerateRandomWavesReturn = false;
        while (t <= 1.0f)
        {
            diffuseNodeArray[z, x] = n;
            n.p = p * t;
            t += Time.deltaTime / duration;
        }
        yield return new WaitForSeconds(duration);
        IEGenerateRandomWavesReturn = true;
    }

    public bool getNeighbours(out DiffuseNodeNeighbours result, int z, int x)
    {
        if (x <= arrayInfo.ARRAY_MAX && x >= arrayInfo.ARRAY_MIN && z <= arrayInfo.ARRAY_MAX && z >= arrayInfo.ARRAY_MIN)
        {
            result.left = diffuseNodeArray[z, x - 1];
            result.left.index = new Vector2Int(z, x - 1);
            result.right = diffuseNodeArray[z, x + 1];
            result.right.index = new Vector2Int(z, x + 1);
            result.up = diffuseNodeArray[z + 1, x];
            result.up.index = new Vector2Int(z + 1, x);
            result.down = diffuseNodeArray[z - 1, x];
            result.down.index = new Vector2Int(z - 1, x);
            return true;
        }
        else
        {
            Debug.LogError("Neighbour out of bound.");
            result = new DiffuseNodeNeighbours();
            return false;
        }
    }

    public DiffuseNode getHighestNeighbour(int z, int x)
    {
        if(z > arrayInfo.ARRAY_MAX || z < arrayInfo.ARRAY_MIN || x > arrayInfo.ARRAY_MAX || x < arrayInfo.ARRAY_MIN)
        {
            Debug.LogError("Max index reached.");
        }
        DiffuseNode result = new DiffuseNode();
        DiffuseNode centerNode = diffuseNodeArray[z, x];
        DiffuseNodeNeighbours n;
        getNeighbours(out n, z, x);

        float topPValue = 0;
        Vector2Int topPIndex = new Vector2Int();
        float lastPValue = 0;

        topPIndex.x = x;
        topPIndex.y = z;

        if (centerNode.p < n.left.p && !n.left.wall)
        {
            topPValue = n.left.p;
            topPIndex.x = x - 1;
            topPIndex.y = z;
            lastPValue = topPValue;
        }
        if (centerNode.p < n.right.p && lastPValue < n.right.p && !n.right.wall)
        {
            topPValue = n.right.p;
            topPIndex.x = x + 1;
            topPIndex.y = z;
            lastPValue = topPValue;
        }
        if (centerNode.p < n.up.p && lastPValue < n.up.p && !n.up.wall)
        {
            topPValue = n.up.p;
            topPIndex.x = x;
            topPIndex.y = z + 1;
            lastPValue = topPValue;
        }
        if (centerNode.p < n.down.p && lastPValue < n.down.p && !n.down.wall)
        {
            topPValue = n.down.p;
            topPIndex.x = x;
            topPIndex.y = z - 1;
            lastPValue = topPValue;
        }
        result = diffuseNodeArray[topPIndex.y, topPIndex.x];
        result.index.x = topPIndex.x;
        result.index.y = topPIndex.y;
        return result; 
    }

    public bool bake = false;

    void FixedUpdate()
    {
        tickTimer += Time.deltaTime;

        foreach (Transform t in bakeObjects)
        {
            Vector2Int index = new Vector2Int();
            Vector2Int scale = new Vector2Int((int)transform.localScale.x, (int)transform.localScale.y);
            for(int y = 0; y < scale.y; y++)
            {
                for (int x = 0; x < scale.x; x++)
                {
                    index = ClampIn2DArray(t.transform.position.x, t.transform.position.y);
                    diffuseNodeArray[index.y, index.x].wall = true;
                }
            }
            
        }

        diffuseNodeArrayExternal = new List<DiffuseNodeExternal>();
        foreach (DiffuseNodeExternal t in _objects)
        {
            if(t.transform)
            {
                DiffuseNodeExternal n = new DiffuseNodeExternal();
                Vector2Int vec = new Vector2Int((int)t.transform.position.z, (int)t.transform.position.x);
                n.transform = t.transform;
                n.name = t.transform.name;
                n.index = ClampIn2DArray(vec);
                n.p = t.p;
                n.color = t.color;
                diffuseNodeArrayExternal.Add(n);
            }
            else
            {
                _objects.Initialize();
            }
        }

        for (int z = arrayInfo.ARRAY_MAX; z >= arrayInfo.ARRAY_MIN; z--)
        {
            for (int x = arrayInfo.ARRAY_MAX; x >= arrayInfo.ARRAY_MIN; x--)
            {
                DiffuseNode t = diffuseNodeArray[z, x];
                DiffuseNodeNeighbours n;
                getNeighbours(out n, z, x);
                
                DiffuseNode result = new DiffuseNode();
                result.position = t.position;
                result.wall = t.wall;
                float d = t.lambda;
                //float psum = diffuseFactor  * (n.left.p + n.right.p + n.up.p + n.down.p / (((n.left.p - t.p)) + ((n.right.p - t.p)) + ((n.up.p - t.p)) + ((n.down.p - t.p))));
                if (t.p > lastMaxPValue) lastMaxPValue = t.p;

                float psum = n.left.p + n.right.p + n.up.p + n.down.p;
                float alpha = 0.25f;
                Color c = new Color(1f - (t.p / maxPValue), (t.p / maxPValue), 0, alpha);
                planeTexture.SetPixel(arrayInfo.ARRAY_WIDTH - x, arrayInfo.ARRAY_WIDTH - z, c);

                planeTexture.SetPixel(0, 0, Color.blue);
                planeTexture.SetPixel(0, arrayInfo.ARRAY_WIDTH, Color.blue);
                planeTexture.SetPixel(arrayInfo.ARRAY_WIDTH, arrayInfo.ARRAY_WIDTH, Color.blue);
                planeTexture.SetPixel(arrayInfo.ARRAY_WIDTH, 0, Color.blue);
                if(t.wall) planeTexture.SetPixel(arrayInfo.ARRAY_WIDTH - x, arrayInfo.ARRAY_WIDTH - z, Color.cyan);
                result.p += 0.25f * psum;
                if (t.wall) result.p = 0.0f;
                result.lambda = t.lambda;
                diffuseNodeArray[z, x] = result;
                //Debug.Log("node[" + z + "," + x + "].p = " + t.p);
                //_topPValueNode = getHighestNeighbour(z, x);
            }
        }
        planeTexture.Apply();
        planeMeshMaterial.mainTexture = planeTexture;
        {
            DiffuseNode n = new DiffuseNode();
            for (int i = 0; i < diffuseNodeArrayExternal.Count; i++)
            {
                DiffuseNodeExternal xn = diffuseNodeArrayExternal[i];
                n.p = xn.p;
                Vector2Int index = ClampIn2DArray(diffuseNodeArrayExternal[i].transform.position.x, diffuseNodeArrayExternal[i].transform.position.z);
                xn.index = index;
                n.index = index;
                n.lambda = xn.lambda;
                float xx = index.x;
                float yy = index.y;
                diffuseNodeArray[index.y, index.x].p = n.p;
            }
        }
        foreach (Transform t in bakeObjects)
        {
            t.localPosition = GetNearestPointOnGrid(t.localPosition, 1.0f);
            //t.localScale = GetNearestPointOnGrid(t.localScale * 1.25f);
            Vector2Int index = new Vector2Int();
            Vector2Int scale = new Vector2Int((int)t.transform.localScale.x, (int)t.transform.localScale.z);
            bool enable = false;
            apply:
            for (float y = -scale.y * 0.5f + 0.5f; y < scale.y * 0.5f + 0.5f; y++)
            {
                for (float x = -scale.x * 0.5f + 0.5f; x < scale.x * 0.5f + 0.5f; x++)
                {
                    if(bake)
                    {
                        bake = false;
                        enable = true;
                        goto apply;     
                    }
                    if(enable)
                    {
                        index = ClampIn2DArray(t.transform.position.x + x, t.transform.position.z + y);
                        diffuseNodeArray[index.y+1, index.x+1].wall = true;
                    }
                }
            }
            if(IEGenerateRandomWavesReturn)
            StartCoroutine(IEGenerateRandomWaves());
        }

        //MoveIn2DArray(ref playerPos);
        //diffuseNodeArray[playerPos.y, playerPos.x].p = _playerCent;

        //if (tickTimer >= _tickRate)
        //{
        //    ClampIn2DArray(ref smthPos);
        //    tnode = getHighestNeighbour(smthPos.y, smthPos.x);
        //    smthPos.y = tnode.index.y;
        //    smthPos.x = tnode.index.x;
        //    tickTimer = 0;

        //    diffuseNodeArray[playerPos2.y, playerPos2.x].p = _playerCent2;
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
            for (int x = 0; x < arrayInfo.ACTUAL_ARRAY_WIDTH; x++)
            {
                for (int z = 0; z < arrayInfo.ACTUAL_ARRAY_WIDTH; z++)
                {
                    if (diffuseNodeArray[z, x].wall)
                    {
                        Gizmos.color = Color.white;
                        Gizmos.DrawWireCube(diffuseNodeArray[z, x].position + Vector3.up * 0.5f, Vector3.one);
                    }
                    else if (false)
                    {
                        DiffuseNode node = diffuseNodeArray[z, x];
                        Color c = new Color(1 - (node.p / maxPValue * diffuseFactor), (node.p / maxPValue * diffuseFactor), 0, 1.0f);
                        Gizmos.color = c;
                        Gizmos.DrawCube(node.position + Vector3.up * 0.25f, new Vector3(1, 1, 1));
                    }
                }
            }
            Gizmos.color = Color.green;
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(diffuseNodeArray[smthPos.y, smthPos.x].position, 1.0f);
            Gizmos.color = Color.grey;
            Gizmos.DrawSphere(tnode.position, 1.0f);

            //boundaries
            //Gizmos.color = Color.yellow;
            //Gizmos.DrawCube(diffuseNodeArray[arrayInfo.ARRAY_MIN, arrayInfo.ARRAY_MIN].position + Vector3.up * 2.5f, new Vector3(0.25f, 5, 0.25f));
            //Gizmos.DrawCube(diffuseNodeArray[arrayInfo.ARRAY_MAX, arrayInfo.ARRAY_MAX].position + Vector3.up * 2.5f, new Vector3(0.25f, 5, 0.25f));
            //Gizmos.DrawCube(diffuseNodeArray[arrayInfo.ARRAY_MIN, arrayInfo.ARRAY_MAX].position + Vector3.up * 2.5f, new Vector3(0.25f, 5, 0.25f));
            //Gizmos.DrawCube(diffuseNodeArray[arrayInfo.ARRAY_MAX, arrayInfo.ARRAY_MIN].position + Vector3.up * 2.5f, new Vector3(0.25f, 5, 0.25f));
            //Gizmos.color = Color.blue;
            //Gizmos.DrawCube(new Vector3(0, 0, 0) + Vector3.up * 2.5f, new Vector3(0.25f, 5, 0.25f));
            //Gizmos.DrawCube(new Vector3(arrayInfo.ARRAY_WIDTH + 1, 0, arrayInfo.ARRAY_WIDTH + 1) + Vector3.up * 2.5f, new Vector3(0.25f, 5, 0.25f));
            //Gizmos.DrawCube(new Vector3(0, 0, arrayInfo.ARRAY_WIDTH + 1) + Vector3.up * 2.5f, new Vector3(0.25f, 5, 0.25f));
            //Gizmos.DrawCube(new Vector3(arrayInfo.ARRAY_WIDTH + 1, 0, 0) + Vector3.up * 2.5f, new Vector3(0.25f, 5, 0.25f));

            //for (int x = 0; x < arrayInfo.ACTUAL_ARRAY_WIDTH; x++)
            //{
            //    DiffuseNode nodeX = diffuseNodeArray[0, x];
            //    DrawText(nodeX.position + Vector3.up * 2.0f, nodeX.index.ToString(), Color.white);
            //    nodeX = diffuseNodeArray[arrayInfo.ACTUAL_ARRAY_WIDTH-1, x];
            //    DrawText(nodeX.position + Vector3.up * 2.0f, nodeX.index.ToString(), Color.white);
            //    for (int z = 0; z < arrayInfo.ACTUAL_ARRAY_WIDTH; z++)
            //    {
            //        DiffuseNode nodeZ = diffuseNodeArray[z, 0];
            //        DrawText(nodeZ.position + Vector3.up * 2.0f, nodeZ.index.ToString(), Color.white);
            //        nodeZ = diffuseNodeArray[z, arrayInfo.ACTUAL_ARRAY_WIDTH-1];
            //        DrawText(nodeZ.position + Vector3.up * 2.0f, nodeZ.index.ToString(), Color.white);
            //    }
            //}
        }
                foreach (var stringpair in Strings)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 32;
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
