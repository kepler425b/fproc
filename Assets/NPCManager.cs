using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[ExecuteInEditMode]
public class NPCManager : MonoBehaviour 
{
    public struct NPCNavInfo
    {
        public float avoidanceRange;
        public Vector3 position;
        public AlienLogic scriptReference;
    };

    [SerializeField] public List<NPCNavInfo> _NPCList = new List<NPCNavInfo>();
    [Range(2, 10)]
    [SerializeField] float _randomAvoidanceRange = 0.0f;
    [SerializeField] bool Randomize = true;
    [SerializeField] bool _debugAvoidanceRange = false;
    [SerializeField] bool _enableDebug = true;
    [SerializeField] bool _debugTargetPoint = true;
    [SerializeField] bool _setRandomTargetForAll = false;
    [SerializeField] Transform _plane;
    [SerializeField] float _tickRate = 0.0f;
    private float _ticker = 0;

  
    public void OnValidate()
    {
        if (Randomize)
        {
            StartCoroutine(IERandomizeParameters());
            Randomize = false;
        }
    }

    Vector3 targetPoint;
    public void SetTarget(Vector3 point)
    {
        targetPoint = point;
        foreach (NPCNavInfo child in _NPCList)
        {
            child.scriptReference.behaviourState.moveTarget = targetPoint;
        }
    }

    IEnumerator IERandomizeParameters()
    {
        foreach (NPCNavInfo child in _NPCList)
        {
            child.scriptReference._avoidanceRange = Random.Range(2, _randomAvoidanceRange);
        }
        yield return null;
    }
    bool resetter = true;

    IEnumerator IESetRandomTargetForAll(float range)
    {
        resetter = false;
        NavMeshHit hit;
        Vector3 randomPoint = Random.insideUnitSphere * range;
        Vector3 result;
        if (NavMesh.SamplePosition(randomPoint, out hit, range, -1))
        {
            result = hit.position;
            foreach (NPCNavInfo child in _NPCList)
            {
                child.scriptReference.behaviourState.moveTarget = hit.position;
            }
        }
        yield return new WaitForSeconds(2.0f);
        resetter = true;
    }
    public void FixedUpdate()
    {
        int i = 0;
        NPCNavInfo info;
        foreach (NPCNavInfo child in _NPCList)
        {
            if (child.scriptReference == null) continue;
            info.position = child.scriptReference.transform.position;
            info.avoidanceRange = child.scriptReference._avoidanceRange;
            info.scriptReference = child.scriptReference;
            _NPCList[i] = info;
            i++;
        }
        if (_setRandomTargetForAll && resetter)
        {
            StartCoroutine(IESetRandomTargetForAll(10.0f));
        }
    }

    public void OnDrawGizmos()
    {
        if (_enableDebug)
        {
            foreach (NPCNavInfo child in _NPCList)
            {
                if (child.scriptReference)
                {
                    if (_debugAvoidanceRange)
                    {
                        Gizmos.DrawWireSphere(child.position, child.avoidanceRange);
                    }
                    Gizmos.DrawLine(child.position, child.scriptReference._NPCAgent.destination);
                    Gizmos.DrawSphere(child.scriptReference._NPCAgent.destination, 0.1f);
                    if (_debugTargetPoint)
                    {
                        Gizmos.DrawSphere(targetPoint, 0.25f);
                    }
                }
            }
        }
    }
}
