using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiffusesNodeMap : MonoBehaviour
{
    [SerializeField] PlayerLogic _playerReference;
    [SerializeField] DiffuseNode _topPValueNode;
    [SerializeField] float _tickRate = 0.1f;
    [SerializeField] Transform _plane;
    [SerializeField] float _playerCent = 5f;
    [SerializeField] float _playerCent2 = 10.0f;
    [SerializeField] DiffuseNodeExternal[] _objects;
    [Range(0.0f, 2.0f)]
    [SerializeField] float diffuseFactor = 0.25f;
    [Range(0.0f, 2.0f)]
    [SerializeField] float thresholdDepth = 0.15f;
    [Range(0.0f, 2.0f)]
    [SerializeField] float threshold = 0.15f;

    public class Debug2String
    {
        public Vector3 pos;
        public string text;
        public Color? color;
        public float eraseTime;
    }

    public List<Debug2String> Strings = new List<Debug2String>();

    private float tickTimer = 0;

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

    DiffuseNode[,] diffuseNodeArray;
    List<DiffuseNodeExternal> diffuseNodeArrayExternal;
    DiffuseNode playerNode, playerNode2;
    DiffuseNode tnode;
    Vector2Int playerPos, playerPos2;
    Vector2Int lastPlayerPos;
    [SerializeField] Vector2Int smthPos;

    public int ARRAY_WIDTH = 4;
    public int AMAX, AMIN;
    float maxPValue = 10f;
    float ratio;
    int density = 18;
    float planeScale;
    Vector3 planePos;
    int[,] Map;

    private void OnValidate()
    {
        
    }

    void Start()
    {

        Map = new [,]{
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
            { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 0, 0, 0, 0, 1},
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

        //planeScale = (int)_plane.transform.localScale.x;
        planePos = _plane.transform.position;
        _plane.transform.localScale = new Vector3((float)ARRAY_WIDTH/10f, 1, (float)ARRAY_WIDTH/10f);
        _plane.transform.position = new Vector3(
            _plane.transform.position.x + ARRAY_WIDTH * 0.5f, 
            _plane.transform.position.y, 
            _plane.transform.position.z + ARRAY_WIDTH * 0.5f);

        //ratio = (float)planeScale / (float)ARRAY_WIDTH;
        AMIN = 2;
        AMAX = ARRAY_WIDTH - 2;
       
        diffuseNodeArray = new DiffuseNode[ARRAY_WIDTH, ARRAY_WIDTH];

        playerNode.p = _playerCent;
        playerNode2.p = _playerCent2;
        playerPos = new Vector2Int(AMIN, AMIN);
        playerPos2 = new Vector2Int(AMAX, AMAX);
        smthPos = new Vector2Int((int)(AMAX * 0.5f), (int)(AMAX * 0.5f));

        int AMIN_TEMP = AMIN - 2;
        int AMAX_TEMP = AMAX + 2;

        ConvertMapTo2DArray(Map, ref diffuseNodeArray);
        //for (int z = AMAX_TEMP-1; z > AMIN_TEMP; z--)
        //{
        //    for (int x = AMAX_TEMP-1; x > AMIN_TEMP; x--)
        //    {
        //        DiffuseNode t = new DiffuseNode();
        //        t.p = 1.0f;
        //        t.diffusionFactor = 1.0f;
        //        //if (x == AMAX_TEMP / 2)
        //            //t.diffusionFactor = 0f;

        //        if (x == AMAX_TEMP)
        //            t.diffusionFactor = 0f;

        //        if (z == AMIN_TEMP)
        //            t.diffusionFactor = 0f;

        //        t.position = new Vector3(planePos.x + x, planePos.y, planePos.z + z);
        //        diffuseNodeArray[z, x] = t;
        //    }
        //}
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
        input.x = Mathf.Clamp(input.x, AMIN, AMAX);
        input.y = Mathf.Clamp(input.y, AMIN, AMAX);
    }

    Vector2Int ClampIn2DArray(Vector2Int input)
    {
        Vector2Int result = new Vector2Int();
        result.x = Mathf.Clamp(input.x, AMIN, AMAX);
        result.y = Mathf.Clamp(input.y, AMIN, AMAX);
        return result;
    }

    Vector2Int ClampIn2DArray(float x, float y)
    {
        Vector2Int result = new Vector2Int();
        result.x = Mathf.Clamp((int)x, AMIN, AMAX);
        result.y = Mathf.Clamp((int)y, AMIN, AMAX);
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
        result.x = Mathf.Clamp(input.x, AMIN+1, AMAX-1);
        result.y = input.y;
        result.z = Mathf.Clamp(input.z, AMIN+1, AMAX-1);
        return result;
    }

    void ConvertMapTo2DArray(int[,] map, ref DiffuseNode[,] array)
    {
        for(int x = 0; x < 32; x++)
        {
            for (int z = 0; z < 32; z++)
            {
                DiffuseNode t = new DiffuseNode();
                t.wall = map[z, x] == 0 ? false : true;
                t.lambda = 1.0f;// map[z, x] == 0 ? 1.0f : 0.0f;
                t.p = map[z, x] == 0 ? 0.0f : 1.0f;
                t.position = new Vector3(planePos.x + x, planePos.y, planePos.z + z);
                diffuseNodeArray[z, x] = t;
            }
        }
    }

    public bool getNeighbours(out DiffuseNodeNeighbours result, int z, int x)
    {
        if (x <= AMAX && x >= AMIN && z <= AMAX && z >= AMIN)
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
        if(z > AMAX || z < AMIN || x > AMAX || x < AMIN)
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

    void FixedUpdate()
    {
        tickTimer += Time.deltaTime;

        for (int z = AMAX; z >= AMIN; z--)
        {
            for (int x = AMAX; x >= AMIN; x--)
            {
                DiffuseNode t = diffuseNodeArray[z, x];
                DiffuseNodeNeighbours n;
                getNeighbours(out n, z, x);

                DiffuseNode result = new DiffuseNode();
                result.position = t.position;
                result.wall = t.wall;
                float d = t.lambda;
                float psum = diffuseFactor * ((n.left.p - t.p)) + ((n.right.p - t.p)) + ((n.up.p - t.p)) + ((n.down.p - t.p));
                

                //float psum = n.left.p + n.right.p + n.up.p + n.down.p;

                result.p = 0.25f * psum;
                if (t.wall) result.p = 0.0f;
                result.lambda = t.lambda;
                diffuseNodeArray[z, x] = result;
                //_topPValueNode = getHighestNeighbour(z, x);
            }
        }
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

    void OnDrawGizmos()
    {
        if (diffuseNodeArray != null)
        {
            for (int x = 0; x < 32; x++)
            {
                for (int z = 0; z < 32; z++)
                {
                    if (diffuseNodeArray[z, x].wall)
                    {
                        Gizmos.color = Color.white;
                        Gizmos.DrawCube(diffuseNodeArray[z, x].position, Vector3.one);
                    }
                    else
                    {
                        DiffuseNode node = diffuseNodeArray[z, x];
                        Color c = new Color(1 - (node.p / maxPValue), (node.p / maxPValue), 0, 1.0f);
                        if (node.p > 0.05f)
                            //Debug.DrawLine(result.position, result.position + Vector3.up * result.p, c, 0.1f);
                        Gizmos.color = c;
                        Gizmos.DrawCube(node.position + Vector3.up * 0.25f, new Vector3(1, 1 * node.p, 1));

                      //  if(tickTimer > _tickRate)
                       // {
                            if (diffuseNodeArray[z, x].p >= threshold && diffuseNodeArray[z, x].p > threshold + thresholdDepth)
                            {
                                Gizmos.color = Color.yellow;
                                Gizmos.DrawCube(diffuseNodeArray[z, x].position + Vector3.up * diffuseNodeArray[z, x].p, Vector3.one);
                                //lastNodePosition = diffuseNodeArray[z, x].position + Vector3.up * diffuseNodeArray[z, x].p;
                            }
                        //    tickTimer = 0f;
                        //}
                    }
                }
            }
            Gizmos.color = Color.green;
            //Gizmos.DrawCube(diffuseNodeArray[playerPos.y, playerPos.x].position, Vector3.one);
            //izmos.DrawCube(diffuseNodeArray[playerPos2.y, playerPos2.x].position, new Vector3(2, 2, 2));
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(diffuseNodeArray[smthPos.y, smthPos.x].position, 1.0f);
            Gizmos.color = Color.grey;
            Gizmos.DrawSphere(tnode.position, 1.0f);

            //boundaries
            Gizmos.color = Color.yellow;
            Gizmos.DrawCube(diffuseNodeArray[AMIN, AMIN].position + Vector3.up * 2.5f, new Vector3(1, 5, 1));
            Gizmos.DrawCube(diffuseNodeArray[AMAX, AMAX].position + Vector3.up * 2.5f, new Vector3(1, 5, 1));
            Gizmos.DrawCube(diffuseNodeArray[AMIN, AMAX].position + Vector3.up * 2.5f, new Vector3(1, 5, 1));
            Gizmos.DrawCube(diffuseNodeArray[AMAX, AMIN].position + Vector3.up * 2.5f, new Vector3(1, 5, 1));
            //Vector3 scale = new Vector3(0f, 1f * node.p * 2f, 1f);
            //DiffuseNodeNeighbours n;
            //getNeighbours(out n, z, x);
            //Gizmos.color = Color.yellow;
            //Gizmos.DrawCube(n.up.position + Vector3.up * 0.0125f, scale);
            //Gizmos.DrawCube(n.down.position + Vector3.up * 0.0125f, scale);
            //Gizmos.DrawCube(n.right.position + Vector3.up * 0.0125f, scale);
            //Gizmos.DrawCube(n.left.position + Vector3.up * 0.0125f, scale);
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
