using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiffusesNodeMap : MonoBehaviour
{
    [SerializeField] PlayerLogic _playerReference;
    [SerializeField] DiffuseNode _topPValueNode;
    [SerializeField] float _tickRate = 0.1f;
    [SerializeField] Transform _plane;
    [SerializeField] float _playerCent = 32f;
    [SerializeField] float _playerCent2 = 1000.0f;
    private float tickTimer = 0;

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
    DiffuseNode playerNode, playerNode2;
    DiffuseNode tnode;
    Vector2Int playerPos, playerPos2;
    Vector2Int lastPlayerPos;
    [SerializeField] Vector2Int smthPos;

    int arrayWidth = 16; //(int)_plane.localScale.x * (int)_plane.localScale.y;
    int AMIN, AMAX;
    float maxPValue = 10.0f;
    float spacing = 4.0f;
    int scaleAdjustion;
    float ratio;
    IEnumerator IENodeIterator()
    {
        yield return new WaitForSeconds(2.0f);
    }

    void Start()
    {
        scaleAdjustion = (int)_plane.transform.localScale.x;
        Vector3 planePos = _plane.transform.position;
        ratio = (float)scaleAdjustion / (float)arrayWidth;
        arrayWidth *= (int)(scaleAdjustion);
        AMIN = 1;
        AMAX = arrayWidth - 1;

        //_playerReference = FindObjectOfType<PlayerLogic>();
        //if (!_playerReference) Debug.LogError("Player reference not set.");

        playerNode.p = _playerCent;
        playerNode2.p = _playerCent2;
        playerPos = new Vector2Int(AMIN, AMIN);
        playerPos2 = new Vector2Int(AMAX, AMAX);
        smthPos = new Vector2Int((int)(AMAX * 0.5f), (int)(AMAX * 0.5f));
        diffuseNodeArray = new DiffuseNode[arrayWidth, arrayWidth];

        for (int z = AMIN-1; z < AMAX+1; z++)
        {
            for (int x = AMIN-1; x < AMAX+1; x++)
            {
                DiffuseNode t = new DiffuseNode();
                t.p = 1.0f;
                t.position = new Vector3((planePos.x + ((AMAX * 0.5f * ratio) - x * ratio)), planePos.y, (planePos.z + ((AMAX * 0.5f * ratio) - z * ratio)));
                diffuseNodeArray[z, x] = t;
            }
        }
        //diffuseNodeArray[1, 1].p = 10f;
       // StartCoroutine(IENodeIterator());
    }
    

    public bool getNeigbours(out DiffuseNodeNeighbours result, int z, int x)
    {
        if (x < AMAX && x > AMIN && z < AMAX && z > AMIN)
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
            DiffuseNode n = new DiffuseNode();
            result = new DiffuseNodeNeighbours();
            n.index = new Vector2Int(1, 1);
            result.down = n;
            result.up = n;
            result.right = n;
            result.left = n;
            return false;
        }
    }

    public DiffuseNode getHighestNeighbour(int z, int x)
    {
        DiffuseNode result = new DiffuseNode();
        DiffuseNode centerNode = diffuseNodeArray[z, x];
        DiffuseNodeNeighbours n;
        getNeigbours(out n, z, x);

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

        for (int z = AMIN; z < AMAX; z++)
        {
            for (int x = AMIN; x < AMAX; x++)
            {
                DiffuseNode t = diffuseNodeArray[z, x];

                DiffuseNodeNeighbours n;
                getNeigbours(out n, z, x);

                DiffuseNode result = new DiffuseNode();
                result.position = t.position;
                float d = 0.05f;
                float psum = ((n.left.p - t.p)) + ((n.right.p - t.p)) + ((n.up.p - t.p)) + ((n.down.p - t.p));
                result.p = d * psum;
                result.diffusionFactor = t.diffusionFactor;
                diffuseNodeArray[z, x] = result;
                _topPValueNode = getHighestNeighbour(z, x);

                Color c = new Color(1 - (t.p / maxPValue), (t.p / maxPValue), 0, 1);
                if(result.p > 0.5f)
                Debug.DrawLine(result.position, result.position + Vector3.up * result.p, c, 0.1f);
            }
        }
        Mathf.Clamp(smthPos.x, AMIN, AMAX);
        Mathf.Clamp(smthPos.y, AMIN, AMAX);
    
        lastPlayerPos = playerPos; 
        if (Input.GetKey(KeyCode.RightArrow))
        {
            if (playerPos.x + 1 < AMAX) playerPos.x += 1;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            if (playerPos.x - 1 >= AMIN) playerPos.x -= 1;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            if (playerPos.y - 1 >= AMIN) playerPos.y -= 1;
        }
        if (Input.GetKey(KeyCode.UpArrow))
        {
            if (playerPos.y + 1 < AMAX) playerPos.y += 1;
        }
        diffuseNodeArray[playerPos.y, playerPos.x].p = _playerCent;
        //diffuseNodeArray[lastPlayerPos.y, lastPlayerPos.x].p = 0f;

        Debug.DrawLine(diffuseNodeArray[smthPos.y, smthPos.x].position,
            diffuseNodeArray[smthPos.y, smthPos.x].position + Vector3.Normalize(tnode.position - diffuseNodeArray[smthPos.y, smthPos.x].position) * tnode.p, Color.blue);
        //Debug.Log("playerPos: " + playerPos.y + ", " + playerPos.x);

        if (tickTimer >= _tickRate)
        {
            tnode = getHighestNeighbour(smthPos.y, smthPos.x);
            smthPos.y = tnode.index.y;
            smthPos.x = tnode.index.x;
            //Debug.Log("smthPos: " + smthPos.y + ", " + smthPos.x);
            tickTimer = 0;

            bool s = Random.Range(0, 16) != 0;
            if (s)
            {
                if (playerPos2.x + 1 < AMAX) playerPos2.x += 1;
            }
            s = Random.Range(0, 18) != 0;
            if (s)
            {
                if (playerPos2.x - 1 >= AMIN) playerPos2.x -= 1;
            }
            s = Random.Range(0, 17) != 0;
            if (s)
            {
                if (playerPos2.y - 1 >= AMIN) playerPos2.y -= 1;
            }
            s = Random.Range(0, 20) != 0;
            if (s)
            {
                if (playerPos2.y + 1 < AMAX) playerPos2.y += 1;
            }
            diffuseNodeArray[playerPos2.y, playerPos2.x].p = _playerCent2;
        }
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
        }
    }
}