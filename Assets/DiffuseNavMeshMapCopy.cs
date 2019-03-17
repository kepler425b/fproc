using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using B83.MeshHelper;

public class DiffuseNavMeshMapCopy : MonoBehaviour
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
    [SerializeField] bool showText = false;
    [Range(-16.0f, 16.0f)]
    [SerializeField] float diffuseBias = 1f;
    [Range(0.0f, 2000.0f)]
    [SerializeField] float testP = 0.15f;
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
        public int polygonIndex;
        public Color color;
        public Vector3 position;
        public Vector3 nodePosition;
        public bool showNeighbours;
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
        public bool skip;
        public int[] adjacentVertexIndex;
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

    void Start()
    {
        GenerateMapFromNavMesh(ref diffuseNodeArray);
    }

    public static void DrawText(Vector3 pos, string text, Color? color = null)
    {
        instance.Strings.Add(new Debug2Mono.Debug2String() { text = text, color = color, pos = pos, eraseTime = Time.time + 0.00025f, });
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

        Mesh m = new Mesh();
        m.vertices = vertices;
        m.triangles = polygons;

        MeshWelder welder = new MeshWelder(m);
        welder.Weld();

        vertices = m.vertices;
        polygons = m.triangles;

        size = polygons.Length;
        array = new DiffuseNode[size];
        arrayColSize = 20;
        arrayRowSize = 20;

        arrayInfo.ARRAY_MAX = size;
        arrayInfo.ARRAY_MIN = 0;
        arrayInfo.ARRAY_HALF_WIDTH = arrayInfo.ARRAY_MAX / 2;

        DiffuseNode t = new DiffuseNode();
        for (int i = arrayInfo.ARRAY_MIN; i < arrayInfo.ARRAY_MAX; i++)
        {
            t.wall = false;
            t.lambda = 1.0f;
            t.p = 1.0f;
            t.position = vertices[polygons[i]];
            t.polygonIndex = polygons[i];
            t.arrayIndex = i;
            array[i] = t;
            array[i].skip = false;
        }
    }

    public List<int> getNeighbours(int polygonIndex)
    {
        List<int> n = new List<int>();
        int index = polygonIndex;
        for (int i = 0; i < size / 3; i++)
        {
            int vert = i * 3;
            if (polygons[vert] == index || polygons[vert + 1] == index || polygons[vert + 2] == index)
            {
                if (polygons[vert] != index)
                {
                    n.Add(vert);
                }
                if (polygons[vert + 1] != index)
                {
                    n.Add(vert + 1);
                }
                if (polygons[vert + 2] != index)
                {
                    n.Add(vert + 2);
                }
            }
        }
        return n;
    }

    public Vector3 getHighestNodeDirection(int index)
    {
        int top = getHighestNeighbour(index);
        Vector3 topPosition = diffuseNodeArray[top].position;
        Vector3 centerPosition = diffuseNodeArray[index].position;
        Vector3 result;
        result = Vector3.Normalize(topPosition - centerPosition);
        return result;
    }

    public Vector3 getHighestNodePosition(int index)
    {
        int top = getHighestNeighbour(index);
        Vector3 topPosition = diffuseNodeArray[top].position;
        Vector3 centerPosition = diffuseNodeArray[index].position;
        Vector3 result;
        result = topPosition;
        return result;
    }

    int getHighestNeighbour(int index)
    {
        DiffuseNode centerNode = diffuseNodeArray[index];
        List<int> nb = new List<int>();
        nb = getNeighbours(index);

        float top_p = 0;
        int top_index  = 0;

        for(int i = 0; i < nb.Count; i++)
        {
            if (centerNode.p < diffuseNodeArray[i].p && diffuseNodeArray[i].p > top_p)
            {
                top_index = i;
                top_p = diffuseNodeArray[i].p;
            }
        }
        Debug.DrawLine(centerNode.position, diffuseNodeArray[top_index].position, Color.blue);
        return top_index;
    }

    public void getClosestNodeToGrid(ref DiffuseNode node, Vector3 position)
    {
        int index = 0;
        float min_distance = 5.5f;
        float distance = 0;
        Vector3 center;
        Vector3 other;
        for (int i = arrayInfo.ARRAY_MIN; i < arrayInfo.ARRAY_MAX; i++)
        {
            if (diffuseNodeArray[i].skip) continue;
            center = new Vector3(position.x, 0, position.z);
            other = new Vector3(diffuseNodeArray[i].position.x, 0, diffuseNodeArray[i].position.z);
            distance = Vector3.Distance(center, other);
            if (distance < min_distance)
            {
                min_distance = distance;
                index = i;
            }
        }
        Debug.DrawLine(position, node.position);
        node = diffuseNodeArray[index];
    }

    public int getClosestNodeToGridIndex(Vector3 position)
    {
        int index = 0;
        float min_distance = 5.5f;
        float distance = 0;
        Vector3 center;
        Vector3 other;
        for (int i = arrayInfo.ARRAY_MIN; i < arrayInfo.ARRAY_MAX; i++)
        {
            if (diffuseNodeArray[i].skip) continue;
            center = new Vector3(position.x, 0, position.z);
            other = new Vector3(diffuseNodeArray[i].position.x, 0, diffuseNodeArray[i].position.z);
            distance = Vector3.Distance(center, other);
            if (distance < min_distance)
            {
                min_distance = distance;
                index = i;
            }
        }
        return index;
    }

 
    float lastMaxPValue = 0;

    List<int> neighbourIndices = new List<int>();
    
    void FixedUpdate()
    {
        tickTimer += Time.deltaTime;

        if (_objects.Length > 0)
        {
            for (int j = 0; j < _objects.Length; j++)
            {
                DiffuseNodeExternal t = _objects[j];
                if (t.transform)
                {
                    DiffuseNodeExternal n = new DiffuseNodeExternal();
                    n.transform = t.transform;
                    n.position = t.transform.position;
                    n.name = t.transform.name;
                    n.p = t.p;
                    n.showNeighbours = t.showNeighbours;
                    n.index = getClosestNodeToGridIndex(n.position);
                    n.color = t.color;
                    _objects[j] = n;
                    diffuseNodeArray[n.index].p = n.p;
                    
                    if (t.showNeighbours)
                    {
                        neighbourIndices = getNeighbours(n.index);
                    }
                }
            }
        }
       
       
        List<int> neighbour_indices = new List<int>();
        for (int i = arrayInfo.ARRAY_MIN; i < arrayInfo.ARRAY_MAX; i++)
        {
            DiffuseNode t = diffuseNodeArray[i];
            DiffuseNode result = new DiffuseNode();
            result = t;
            neighbour_indices = getNeighbours(t.polygonIndex);

            float d = t.lambda;
            //float psum = diffuseFactor  * (n.left.p + n.right.p + n.up.p + n.down.p / (((n.left.p - t.p)) + ((n.right.p - t.p)) + ((n.up.p - t.p)) + ((n.down.p - t.p))));
            if (t.p > lastMaxPValue) lastMaxPValue = t.p;

            float psum = 0f;
            float psumDelta = 0f;
            for (int j = 0; j < neighbour_indices.Count; j++)
            {
                psum += diffuseNodeArray[neighbour_indices[j]].p;
                //psumDelta += diffuseNodeArray[neighbour_indices[j]].p - diffuseNodeArray[i].p;
            }
            //result.p = diffuseFactor * (psum / psumDelta);
            result.p = psum / (neighbour_indices.Count * diffuseBias);
            float alpha = 0.25f;
            Color c = new Color(1f - (t.p / maxPValue), (t.p / maxPValue), 0, alpha);

            //planeTexture.SetPixel(arrayInfo.ARRAY_HALF_WIDTH + (arrayInfo.ARRAY_WIDTH - x), arrayInfo.ARRAY_HALF_WIDTH + (arrayInfo.ARRAY_WIDTH - z), c);
            //planeTexture.SetPixel(0, 0, Color.blue);
            //planeTexture.SetPixel(0, arrayInfo.ARRAY_WIDTH, Color.blue);
            //planeTexture.SetPixel(arrayInfo.ARRAY_WIDTH, arrayInfo.ARRAY_WIDTH, Color.blue);
            //planeTexture.SetPixel(arrayInfo.ARRAY_WIDTH, 0, Color.blue);
            diffuseNodeArray[i] = result;
            //Debug.Log(result.p);
        }
    }
    //IEnumerator LoopIE()
    //{
    //    int k = 0;
    //    while (true)
    //    {
    //        for (int i = arrayInfo.ARRAY_MIN; i < arrayInfo.ARRAY_MAX; i++)
    //        {
    //            DiffuseNode t = diffuseNodeArray[i];
    //            DiffuseNode result = new DiffuseNode();
    //            result = t;
    //            neighbour_indices = getNeighbours(t.polygonIndex);

    //            float d = t.lambda;
    //            //float psum = diffuseFactor  * (n.left.p + n.right.p + n.up.p + n.down.p / (((n.left.p - t.p)) + ((n.right.p - t.p)) + ((n.up.p - t.p)) + ((n.down.p - t.p))));
    //            if (t.p > lastMaxPValue) lastMaxPValue = t.p;

    //            float psum = 0f;
    //            for (int j = 0; j < neighbour_indices.Count; j++)
    //            {
    //                psum += diffuseNodeArray[neighbour_indices[j]].p;
    //            }
    //            result.p = psum / _tickRate;

    //            float alpha = 0.25f;
    //            Color c = new Color(1f - (t.p / maxPValue), (t.p / maxPValue), 0, alpha);

    //            diffuseNodeArray[i] = result;
    //            //planeTexture.SetPixel(arrayInfo.ARRAY_HALF_WIDTH + (arrayInfo.ARRAY_WIDTH - x), arrayInfo.ARRAY_HALF_WIDTH + (arrayInfo.ARRAY_WIDTH - z), c);

    //            //planeTexture.SetPixel(0, 0, Color.blue);
    //            //planeTexture.SetPixel(0, arrayInfo.ARRAY_WIDTH, Color.blue);
    //            //planeTexture.SetPixel(arrayInfo.ARRAY_WIDTH, arrayInfo.ARRAY_WIDTH, Color.blue);
    //            //planeTexture.SetPixel(arrayInfo.ARRAY_WIDTH, 0, Color.blue);
    //            yield return new WaitForSeconds(_tickRate);
    //            neighbour_indices.Clear();
    //            k = +3;
    //        }
    //    }
    //}

    

    void OnDrawGizmos()
    {
        if (diffuseNodeArray != null && _debugDiffuseMap)
        {
            for (int i = 0; i < arrayInfo.ARRAY_MAX; i++)
            {
                DiffuseNode node = diffuseNodeArray[i];
                Color c = new Color(1 - (node.p / maxPValue), (node.p / maxPValue), 0, 1.0f);
                Gizmos.color = c;
                float scaleY = node.p / maxPValue;
                Gizmos.DrawCube(node.position + Vector3.up * scaleY * 0.5f, new Vector3(0.25f, 1.0f * scaleY, 0.25f));
            }
            if (_objects.Length > 0)
            {
                for (int j = 0; j < _objects.Length; j++)
                {
                    if (_objects[j].transform)
                    {
                        DiffuseNode n = diffuseNodeArray[_objects[j].index];
                        Gizmos.color = _objects[j].color;
                        Gizmos.DrawCube(n.position, new Vector3(0.75f, 0.75f, 0.75f));
                        if(_objects[j].showNeighbours)
                        {
                            Gizmos.color = new Color(Color.yellow.r, Color.yellow.g, Color.yellow.b, 0.35f);
                            for (int k = 0; k < neighbourIndices.Count; k++)
                            {
                                Gizmos.DrawCube(diffuseNodeArray[neighbourIndices[k]].position, new Vector3(0.75f, 0.75f, 0.75f));
                            }
                        }
                    }
                }
                neighbourIndices.Clear();
            }
        }
        if(showText)
        for (int i = 0; i < arrayInfo.ARRAY_MAX; i++)
        {
            DiffuseNode node = diffuseNodeArray[i];
            float scaleY = node.p / maxPValue;
            DrawText(node.position + (Vector3.up * scaleY * 0.5f) + Vector3.up * 2.0f, node.p.ToString("0.00"), Color.white);
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
