using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    [SerializeField] GameObject _NPC;
    [SerializeField] int _amount;
    [SerializeField] float _radius;
    [Range(0f, 10f)]
    [SerializeField] float _randomDelayMax;
    Transform parent;
    NPCManager NPCManager;

    IEnumerator SpawnNPC()
    {
        NPCManager.NPCNavInfo info = new NPCManager.NPCNavInfo();
        AlienLogic NPCRef;
        PlayerLogic PlayerRef = FindObjectOfType<PlayerLogic>();
        Vector3 previousPos = Vector3.zero;
        for (int i = 0; i < _amount; i++)
        {
            Vector3 p = previousPos + Random.insideUnitSphere * _radius;
            previousPos = p;
            GameObject o = Instantiate(_NPC);
            if (parent)
            {
                o.transform.parent = parent;
            }
            o.transform.position = p;
#if false
            SkinnedMeshRenderer smr = o.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr)
            {
                int index = Random.Range(0, 2);
                Material mat = smr.materials[index];
                smr.material = mat;
                AlienLogic script = o.GetComponent<AlienLogic>();
                script.skinnedMeshMaterialIndex = index;
                script._agentTarget = PlayerRef.transform;
            }
#endif
#if false
            o.transform.localScale *= Random.Range(0.25f, 5.5f);
#endif
            NPCRef = o.GetComponent<AlienLogic>();
            info.avoidanceRange = NPCRef._avoidanceRange;
            info.scriptReference = NPCRef;
            NPCManager._NPCList.Add(info);
            yield return new WaitForSeconds(Random.Range(0, _randomDelayMax));
        }
    }

    private void Awake()
    {
        NPCManager = GetComponentInParent<NPCManager>();
        if (NPCManager == null) Debug.LogError("NPCManager is null");

        parent = NPCManager.transform;
    }

    void Start()
    {
        StartCoroutine(SpawnNPC());    
    }
}
