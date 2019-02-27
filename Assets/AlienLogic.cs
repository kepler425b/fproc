﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine.EventSystems;

public class AlienLogic : MonoBehaviour
{
    [SerializeField] Transform _agentTarget;
    [SerializeField] public NavMeshAgent _NPCAgent;
    [SerializeField] bool _ignoreObstacles;
    [SerializeField] bool _debugNPCRayCast = false;
    [SerializeField] bool _debugNPCSearchRay = true;
    [SerializeField] bool _debugNPCViewFrustum = true;
    [SerializeField] bool _debugGizmos = false;
    [SerializeField] bool _doesNPCSeePlayer;
    [SerializeField] bool _rayHitObject;
    [SerializeField] float _viewRadius = 5.0f;
    [SerializeField] float viewArcSegments = 8.0f;
    [SerializeField] float _viewCurveAmount = 45.0f;
    [SerializeField] int _angleBetweenPlayer;
    [SerializeField] float _health = 100000f;
    [SerializeField] float _healthDelta;
    [SerializeField] float _maxDamageDelta = 20.0f;
    [SerializeField] float _maxHitDelta = 0.1f;
    [SerializeField] float timeBetweenHits;
    [SerializeField] Transform _dummyObject;
    [SerializeField] DiffusesNodeMap _diffuseNodeMap;

    [Range(2, 20)]
    [SerializeField] public float _avoidanceRange;

    [SerializeField] SoundManager _soundManager;
    [SerializeField] GameObject _ragdoll;
    [Range(0.0f, 1.0f)]
    [SerializeField] float _normalizedTime = 0.0f;
    Animator _animator;
    public NPCManager NPCManager;

    float attackRadius = 1.8f;
    float hitRate = 0.5f;
    float hitTimer;
    float distanceToEnemy;

    BoxCollider NPCBoxCollider;
    SkinnedMeshRenderer skinnedMeshRenderer;
    Rigidbody rb;
    Color originalColor;
    CPMovement player;
    PlayerLogic playerLogic;
    Vector3 originalScale;
    RaycastHit NPCAgentRayCastHit;
    Vector3 NPCEyeSight;
    Vector3 playerPostion;
    List<Vector3> viewArcNodes;
    List<Vector3> parabolicPointList;
    float arcCalcAngle;
    float viewArcOffset = 90f;
    Vector3 parabolicPoint;
    Vector3 direction;

    Node behaviourTree;
    public Context behaviourState;

    //NPCManager init
    NPCManager.NPCNavInfo info = new NPCManager.NPCNavInfo();

    private void Awake()
    {
        NPCManager = FindObjectOfType<NPCManager>();
        if (NPCManager == null) Debug.LogError("NPCManager is null");
        _diffuseNodeMap = FindObjectOfType<DiffusesNodeMap>();
    }

    void Start()
    {
        player = FindObjectOfType<CPMovement>().GetComponent<CPMovement>();
        if (player == null)
        {
            Debug.LogError("CPMovement or Player Logic are not assigned.");
        }
        else
        {
            playerLogic = player.GetComponent<PlayerLogic>();
        }
        if (!_agentTarget) _agentTarget = FindObjectOfType<CPMovement>().transform;

        NPCManager = FindObjectOfType<NPCManager>();

        _diffuseNodeMap = FindObjectOfType<DiffusesNodeMap>();
        skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

        _animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        viewArcNodes = new List<Vector3>();
        parabolicPointList = new List<Vector3>();
        NPCBoxCollider = GetComponent<BoxCollider>();
        originalScale = transform.localScale;
        //meshRenderer = GetComponentInChildren<MeshRenderer>();
        //originalColor = meshRenderer.material.color;

        behaviourState = new Context();  // optionally add things you might need access to in your leaf nodes
        behaviourState.enemy = _agentTarget.GetComponent<PlayerLogic>();
        behaviourState.me = this;
        behaviourTree = CreateBehaviourTree();
        behaviourState._originalRadius = _NPCAgent.stoppingDistance;

        info.avoidanceRange = _avoidanceRange;
        info.scriptReference = this;
        NPCManager._NPCList.Add(info);
    }

    Node CreateBehaviourTree()
    {
        Sequence moveRandomly = new Sequence("moveRandomly",
            new MoveForward());

        Sequence attackEnemy = new Sequence("attackEnemy",
            new SetTargetEnemy(),
            new MoveForward(),
            new AttackEnemy());

        Selector fightOrFlight = new Selector("fightOrFlight",
            attackEnemy, moveRandomly);

        Repeater repeater = new Repeater(moveRandomly);

        return repeater;
    }

    public float DistanceTo(Vector3 target)
    {
        float result = Vector3.Distance(transform.position, target);
        return result;
    }

    public void LookAt(Vector3 target)
    {

    }

    public void MoveForward(bool state)
    {
        if (state)
        {
            ///if(!_NPCAgent.hasPath)
            _NPCAgent.SetDestination(behaviourState.moveTarget);
        }
    }

    public void MoveForwardDiffuseMap(bool state)
    {
        Debug.Log("_NPCAgent.hasPath: " + _NPCAgent.hasPath);
        if (state)
        {
            //Vector3 p = _diffuseNodeMap.getHighestNodePosition(transform.position);
            Vector3 dir = _diffuseNodeMap.getHighestNodeDirection(transform.position);
            _NPCAgent.SetDestination(transform.position + dir);
        }
    }

    void ScanPlayer()
    {
        viewArcNodes.Clear();
        arcCalcAngle = viewArcOffset + (-_viewCurveAmount / 2.0f);
        for (int i = 0; i < viewArcSegments + 1; i++)
        {
            float x = Mathf.Cos(arcCalcAngle * Mathf.Deg2Rad) * _viewRadius;
            float z = Mathf.Sin(arcCalcAngle * Mathf.Deg2Rad) * _viewRadius;

            viewArcNodes.Add(transform.position + (transform.right * x) + (transform.forward * z));
            arcCalcAngle += _viewCurveAmount / viewArcSegments;
        }

        for (int i = 0; i < viewArcNodes.Count - 1; i++)
        {
            if (_debugNPCViewFrustum)
            {
                Debug.DrawLine(viewArcNodes[i], viewArcNodes[i + 1], Color.cyan, 0.25f);
                Debug.DrawLine(transform.position, viewArcNodes[0], Color.cyan, 0.25f);
                Debug.DrawLine(transform.position, viewArcNodes[viewArcNodes.Count - 1], Color.cyan, 0.25f);
            }
            RaycastHit NPCAgentRayCastHit;
            Ray ray = new Ray(transform.position, Vector3.Normalize(viewArcNodes[i] - transform.position));
            if (_rayHitObject = Physics.Raycast(ray, out NPCAgentRayCastHit, Mathf.Infinity))
            {
                if (NPCAgentRayCastHit.collider.gameObject.tag == "Player")
                {
                    _doesNPCSeePlayer = true;
                    playerPostion = NPCAgentRayCastHit.collider.gameObject.transform.position;
                    //_NPCAgent.SetDestination(playerPostion);
                    break;
                }
                else
                {
                    _doesNPCSeePlayer = false;
                }
            }
            else
            {
                _doesNPCSeePlayer = false;
            }
        }
        if (_debugNPCSearchRay)
        {
            Debug.DrawRay(transform.position, NPCEyeSight * 2.0f, Color.blue, 0.25f);
        }
    }

    void FixedUpdate()
    {
        transform.position = _diffuseNodeMap.ClampIn2DArray(transform.position);
        behaviourTree.Behave(behaviourState);
        foreach (NPCManager.NPCNavInfo p in NPCManager._NPCList)
        {
            Vector3 otherPos = p.scriptReference._NPCAgent.transform.position;
            if (gameObject.GetInstanceID() == p.scriptReference.GetInstanceID()) continue;

            float distance = Vector3.Distance(_NPCAgent.transform.position, otherPos);
            Vector3 direction = Vector3.Normalize(p.position - _NPCAgent.transform.position);
            Debug.DrawLine(transform.position + Vector3.up * 2.0f, (transform.position + Vector3.up * 2.0f) + direction * distance, Color.red);

            Vector3 optimalPosition = _NPCAgent.transform.position;
            Vector3 optimalPosition2 = p.scriptReference._NPCAgent.transform.position;
            //Debug.Log("distance: " + distance);
            if (distance <= _avoidanceRange + p.avoidanceRange)
            {
                //optimalPosition = _NPCAgent.transform.position;
                //Vector3 direction = -Vector3.Normalize(p.position - transform.position);
                //Debug.DrawLine(transform.position + Vector3.up * 2.0f, (transform.position + Vector3.up * 2.0f) + direction, Color.red);
                p.scriptReference.behaviourState.offsetFromOther = transform.position + Vector3.Normalize(_NPCAgent.transform.position - optimalPosition2);
                //Debug.DrawLine(transform.position + Vector3.up * 4.0f, Vector3.up * 4.0f + behaviourState.offsetFromOther * distance, Color.magenta);
            }
            //else behaviourState.offsetFromOther = Vector3.zero;
        }
    }

    void Update()
    {
        direction = Vector3.Normalize(_agentTarget.transform.position - transform.position);
        behaviourState.enemyDirection = direction;
        distanceToEnemy = DistanceTo(_agentTarget.transform.position);
        float dotOffseted = -Vector3.Dot(transform.forward, direction);
        float distance = Vector3.Distance(_agentTarget.transform.position, transform.position);
        _angleBetweenPlayer = (int)(Mathf.Acos(dotOffseted) * Mathf.Rad2Deg);


        //_NPCAgent.SetDestination(_agentTarget.position - direction * attackRadius * 0.90f);

        //FindPointBehindPlayer();

        float speed = _NPCAgent.speed;
        float velocity = Vector3.Magnitude(_NPCAgent.velocity) / speed;
        float upVelocity = _NPCAgent.velocity.z / speed;
        float normalizedSpeed = 1 - velocity;
        _animator.SetFloat("Normalized Speed", normalizedSpeed);
        _animator.SetFloat("Velocity", velocity);
        _animator.SetFloat("Up Velocity", upVelocity);
        hitTimer += Time.deltaTime;
        damageTimer += Time.deltaTime;
    }

    public void ShowDebugPoint()
    {
        StartCoroutine(IEShowDebugPoint(1, behaviourState.moveTarget));
    }

    bool showPoint = false;
    IEnumerator IEShowDebugPoint(float duration, Vector3 pos)
    {
        showPoint = true;
        yield return new WaitForSeconds(duration);
        showPoint = false;
    }

    public IEnumerator IEStopAgent(float duration)
    {
        _NPCAgent.isStopped = true;
        _NPCAgent.velocity = Vector3.zero;
        float t = 0.0f;
        while (t <= hitRate)
        {
            _NPCAgent.velocity = Vector3.zero;
            _NPCAgent.isStopped = true;
            t += Time.deltaTime;
            yield return null;
        }
        _NPCAgent.isStopped = false;
    }

    bool isAttacking;
    IEnumerator IEAttack(float hitRate)
    {
        _animator.SetTrigger("Attack");
        playerLogic.receiveDamage(10);
        float t = 0.0f;
        isAttacking = true;
        while (t <= hitRate)
        {
            _NPCAgent.velocity = Vector3.zero;
            _NPCAgent.isStopped = true;
            t += Time.deltaTime;
            yield return null;
        }
        _NPCAgent.isStopped = false;
        isAttacking = false;
    }

    public void Attack()
    {
        if(!isAttacking) StartCoroutine(IEAttack(hitRate));
    }

    public void FindPointBehindPlayer()
    {
        float radius = 2.0f;
        Vector3 result = Vector3.zero;
        if (RandomPoint(_agentTarget.position + direction * radius, radius, out result))
        {
            _NPCAgent.SetDestination(result);
        }
    }

    public void FindPointOffsight()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        if (_debugNPCRayCast)
            Debug.DrawRay(transform.position, ray.direction * 4.0f, Color.red, 1.0f);

        if (Physics.Raycast(ray, out NPCAgentRayCastHit))
        {
            if (NPCAgentRayCastHit.collider.gameObject.tag == "Player")
            {
                _doesNPCSeePlayer = true;
                playerPostion = NPCAgentRayCastHit.collider.gameObject.transform.position;
                _NPCAgent.SetDestination(playerPostion);
                if (_debugNPCRayCast)
                    Debug.DrawRay(transform.position, playerPostion, Color.green, 1.0f);
            }
            else
            {
                _doesNPCSeePlayer = false;
            }
        }
    }

    public float range = 10.0f;
    bool RandomPoint(Vector3 center, float range, out Vector3 result, int areaMask = -1)
    {
        Vector3 randomPoint = center + Random.insideUnitSphere * range;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, range, areaMask))
        {
            result = hit.position;
            return true;
        }
        else
        {
            result = Vector3.zero;
            return false;
        }
    }

    Vector3 point;
    float lastHitTime;
    float nowHitTime;
    float lastHPAmount;
    float nowHPAmout;

    Vector3 coverPoint;
    Vector3 eyeSightOffsetY = new Vector3(0.0f, 0.25f, 0.0f);
    float coverLookUpRange = 50.0f;

    bool LookForCover()
    {
        Vector3 center = transform.position;
        RaycastHit rayHit;
        Ray ray = new Ray();
        int rayMask = 1 << 14;
        rayMask = ~rayMask;
        for (int i = 0; i < 5; i++)
        {
            Vector3 randomPoint = center + Random.insideUnitSphere * coverLookUpRange;
            NavMeshHit navHit;

            if (NavMesh.SamplePosition(randomPoint, out navHit, coverLookUpRange, NavMesh.AllAreas))
            {
                Vector3 directionToPlayer = Vector3.Normalize(navHit.position - transform.position);
                ray.direction = directionToPlayer;
                ray.origin = navHit.position + eyeSightOffsetY;
                if (Physics.Raycast(ray, out rayHit, Mathf.Infinity, rayMask))
                {
                    coverPoint = rayHit.point;
                    ray.origin = coverPoint;
                    ray.direction = -directionToPlayer;
                    Debug.DrawLine(transform.position, coverPoint, Color.magenta, 2.0f);

                    if (Physics.Raycast(ray, out rayHit, Mathf.Infinity, rayMask))
                    {
                        coverPoint = rayHit.point + rayHit.normal;
                        Debug.DrawLine(transform.position, coverPoint, Color.green, 2.0f);
                        _NPCAgent.SetDestination(coverPoint);
                        return true;
                    }
                }
            }
        }
        return false;
    }

    static bool recovered = true;
    float damageTimer = 0.0f;
    float damageRate = 0.5f;

    public void OnHit(float amount)
    {
        if(damageTimer >= damageRate)
        {
            StartCoroutine(IEStopAgent(0.5f));
            nowHitTime = Time.time;
            _animator.SetTrigger("Hit");
            _animator.SetInteger("HitAnimationIndex", Random.Range(0, 2));
            nowHPAmout = _health;
            _healthDelta = Mathf.Abs((lastHPAmount - nowHPAmout) / timeBetweenHits);
            lastHPAmount = nowHPAmout;
            _soundManager.Hit();
            timeBetweenHits = nowHitTime - lastHitTime;
            lastHitTime = nowHitTime;

            _health -= amount;
            if (_health <= 0.0f)
            {
                GameObject o = Instantiate(_ragdoll);
                o.transform.position = transform.position;
                o.transform.rotation = transform.rotation;
                o.transform.localScale = transform.localScale;
                SkinnedMeshRenderer smr = o.transform.GetComponentInChildren<SkinnedMeshRenderer>();
                if (smr) smr.material = skinnedMeshRenderer.sharedMaterial;

                StopAllCoroutines();
                Destroy(gameObject, 0.1f);
                //_health = 100.0f;
            }
            damageTimer = 0.0f;
        }
    }
}

    
