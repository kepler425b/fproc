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
        public Vector2Int index;
        public Color color;
    }

    public struct DiffuseNode
    {
        public float p;
        public Vector3 position;
        public bool wall;
        public Vector2Int index;
        public float diffusionFactor;
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

    void Start()
    {
        diffuseNodeArrayExternal = new List<DiffuseNodeExternal>();
        foreach (DiffuseNodeExternal t in _objects)
        {
            DiffuseNodeExternal n = new DiffuseNodeExternal();
            Vector2Int vec = new Vector2Int((int)t.transform.position.z, (int)t.transform.position.x);
            n.transform = t.transform;
            n.name = t.transform.name;
            n.index = ClampIn2DArray(vec);
            n.p = t.p;
            n.color = Color.yellow;
            diffuseNodeArrayExternal.Add(n);
        }

        ARRAY_WIDTH = 20;
        //planeScale = (int)_plane.transform.localScale.x;
        Vector3 planePos = _plane.transform.position;
        _plane.transform.localScale = new Vector3((float)ARRAY_WIDTH/10f, 1, (float)ARRAY_WIDTH/10f);
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

        for (int z = AMIN_TEMP; z < AMAX_TEMP; z++)
        {
            for (int x = AMIN_TEMP; x < AMAX_TEMP; x++)
            {
                DiffuseNode t = new DiffuseNode();
                t.p = 1.0f;
                t.diffusionFactor = 1.0f;
                if (x == AMAX_TEMP / 2)
                    t.diffusionFactor = 0f;

                if (x == AMAX_TEMP)
                    t.diffusionFactor = 0f;

                if (z == AMIN_TEMP)
                    t.diffusionFactor = 0f;

                t.position = new Vector3((planePos.x + (AMAX_TEMP * 0.5f) - x), planePos.y, (planePos.z + (AMAX_TEMP * 0.5f) - z));
                diffuseNodeArray[z, x] = t;
            }
        }
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

        if (centerNode.p < n.left.p)
        {
            topPValue = n.left.p;
            topPIndex.x = x - 1;
            topPIndex.y = z;
            lastPValue = topPValue;
        }
        else if (centerNode.p < n.right.p && lastPValue < n.right.p)
        {
            topPValue = n.right.p;
            topPIndex.x = x + 1;
            topPIndex.y = z;
            lastPValue = topPValue;
        }
        else if (centerNode.p < n.up.p && lastPValue < n.up.p)
        {
            topPValue = n.up.p;
            topPIndex.x = x;
            topPIndex.y = z + 1;
            lastPValue = topPValue;
        }
        else if (centerNode.p < n.down.p && lastPValue < n.down.p)
        {
            topPValue = n.down.p;
            topPIndex.x = x;
            topPIndex.y = z - 1;
            lastPValue = topPValue;
        }
        else
        {
            topPIndex.x = x;
            topPIndex.y = z;
        }
        result = diffuseNodeArray[topPIndex.y, topPIndex.x];
        result.index.x = topPIndex.x;
        result.index.y = topPIndex.y;
        return result; 
    }

    void FixedUpdate()
    {
        tickTimer += Time.deltaTime;

        for (int z = AMIN; z <= AMAX; z++)
        {
            for (int x = AMIN; x <= AMAX; x++)
            {
                DiffuseNode t = diffuseNodeArray[z, x];

                DiffuseNodeNeighbours n;
                getNeighbours(out n, z, x);

                DiffuseNode result = new DiffuseNode();
                result.position = t.position;
                float d = t.diffusionFactor;
                //float psum = ((n.left.p - t.p)) + ((n.right.p - t.p)) + ((n.up.p - t.p)) + ((n.down.p - t.p));
                float psum = n.left.p + n.right.p + n.up.p + n.down.p;
                result.p = d * 0.25f * psum;
                result.diffusionFactor = t.diffusionFactor;
                diffuseNodeArray[z, x] = result;
                //_topPValueNode = getHighestNeighbour(z, x);

                Color c = new Color(1 - (t.p / maxPValue), (t.p / maxPValue), 0, 1);
                if (result.p > 0.05f)
                    Debug.DrawLine(result.position, result.position + Vector3.up * result.p, c, 0.1f);
            }
        }

        for (int i = 0; i < diffuseNodeArrayExternal.Count; i++)
        {
            DiffuseNodeExternal n = diffuseNodeArrayExternal[i];
            //n.transform = diffuseNodeArrayExternal[i].transform;
            n.p = diffuseNodeArrayExternal[i].p;
            Vector2Int index = ClampIn2DArray(Mathf.Abs(diffuseNodeArrayExternal[i].transform.position.x) + AMAX * 0.5f, Mathf.Abs(diffuseNodeArrayExternal[i].transform.position.y) + AMAX * 0.5f);
            n.index = index;
            float xx = index.x;
            float yy = index.y;
            diffuseNodeArray[index.x, index.y].p = n.p;
        }


        MoveIn2DArray(ref playerPos);
        diffuseNodeArray[playerPos.y, playerPos.x].p = _playerCent;

        if (tickTimer >= _tickRate)
        {
            ClampIn2DArray(ref smthPos);
            tnode = getHighestNeighbour(smthPos.y, smthPos.x);
            smthPos.y = tnode.index.y;
            smthPos.x = tnode.index.x;
            tickTimer = 0;

            diffuseNodeArray[playerPos2.y, playerPos2.x].p = _playerCent2;
        }
        Debug.DrawLine(diffuseNodeArray[smthPos.y, smthPos.x].position,
            diffuseNodeArray[smthPos.y, smthPos.x].position + Vector3.Normalize(tnode.position - diffuseNodeArray[smthPos.y, smthPos.x].position) * tnode.p, Color.blue);

    }

    void OnDrawGizmos()
    {
        if(diffuseNodeArray != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawCube(diffuseNodeArray[playerPos.y, playerPos.x].position, Vector3.one);
            Gizmos.DrawCube(diffuseNodeArray[playerPos2.y, playerPos2.x].position, new Vector3(2, 2, 2));
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

            foreach (DiffuseNodeExternal n in diffuseNodeArrayExternal)
            {
                Vector3 scale = new Vector3(2, 2, 2);
                Gizmos.DrawCube(diffuseNodeArray[n.index.y, n.index.x].position, scale);
                DrawText(diffuseNodeArray[n.index.y, n.index.x].position, n.name, Color.white);
                DrawText(diffuseNodeArray[n.index.y, n.index.x].position + Vector3.up * 4.0f, n.index.ToString(), Color.blue);
                DrawText(diffuseNodeArray[n.index.y, n.index.x].position + Vector3.up * 12.0f, n.transform.position.ToString(), Color.red);
                DrawText(diffuseNodeArray[n.index.y, n.index.x].position + Vector3.up * 12.0f, n.transform.position.ToString(), Color.red);
            }
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
