using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine.EventSystems;

public class AlienLogic : MonoBehaviour
{
    [SerializeField] public Transform _agentTarget;
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
    [SerializeField] DiffusesNodeMap _diffuseNavMap;
    [SerializeField] float _parabolaJumpHeight = 2.0f;
    [SerializeField] float _parabolaJumpDuration = 2.0f;

    [Range(2, 20)]
    [SerializeField] public float _avoidanceRange;

    [SerializeField] SoundManager _soundManager;
    [SerializeField] GameObject _ragdoll;
    [Range(0.0f, 10.0f)]
    [SerializeField] float animationSpeed = 1.0f;
    [SerializeField] float jumpDelay = 0.2f;
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
    Vector3 direction;
    public int skinnedMeshMaterialIndex = 0;

    Node behaviourTree;
    public Context behaviourState;

    //NPCManager init
    NPCManager.NPCNavInfo info = new NPCManager.NPCNavInfo();

    private Vector3 velocityDeltaStart;
    private Vector3 velocityDeltaEnd;
    [SerializeField] Vector3 velocityDelta;

    private void OnEnable()
    {
        NPCManager = FindObjectOfType<NPCManager>();
        if (NPCManager == null) Debug.LogError("NPCManager is null");

        player = FindObjectOfType<CPMovement>().GetComponent<CPMovement>();
        if (player == null)
        {
            Debug.LogError("CPMovement or Player Logic are not assigned.");
        }
        else
        {
            playerLogic = player.GetComponent<PlayerLogic>();
        }

        _diffuseNavMap = FindObjectOfType<DiffusesNodeMap>();
        skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        skinnedMeshRenderer.material = skinnedMeshRenderer.sharedMaterials[skinnedMeshMaterialIndex];
        _animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        NPCBoxCollider = GetComponent<BoxCollider>();

        viewArcNodes = new List<Vector3>();
        originalScale = transform.localScale;

        behaviourState = new Context();  // optionally add things you might need access to in your leaf nodes
        behaviourState.enemy = playerLogic;
        behaviourState.me = this;
        behaviourTree = CreateBehaviourTree();
        behaviourState._originalRadius = _NPCAgent.stoppingDistance;

        info.avoidanceRange = _avoidanceRange;
        info.scriptReference = this;
        NPCManager._NPCList.Add(info);
        _diffuseNavMap.RegisterObject(transform);
    }

    void Start()
    {
        
    }
    

    void FixedUpdate()
    {
        behaviourTree.Behave(behaviourState);
    }

    void Update()
    {
        velocityDeltaStart = transform.position;

        if (_agentTarget)
        {
            direction = Vector3.Normalize(_agentTarget.transform.position - transform.position);
            behaviourState.enemyDirection = direction;
            distanceToEnemy = DistanceTo(_agentTarget.transform.position);
            float dotOffseted = -Vector3.Dot(transform.forward, direction);
            float distance = Vector3.Distance(_agentTarget.transform.position, transform.position);
            _angleBetweenPlayer = (int)(Mathf.Acos(dotOffseted) * Mathf.Rad2Deg);
        }

        float speed = _NPCAgent.speed;
        float animationSyncRatio = 0.6f / 1.0f;
        float velocity = Vector3.Magnitude(_NPCAgent.velocity) / speed;
        animationSpeed = speed * animationSyncRatio;
        float normalizedSpeed = velocity * animationSpeed;
        _animator.SetFloat("Normalized Speed", normalizedSpeed);
        _animator.SetFloat("Velocity", velocity);
        _animator.SetFloat("Up Velocity", velocityDelta.y);
        hitTimer += Time.deltaTime;
        damageTimer += Time.deltaTime;
        jumpTimer += Time.deltaTime;
        velocityDelta = velocityDeltaStart - velocityDeltaEnd;
        velocityDeltaEnd = velocityDeltaStart;
        Debug.Log("_NPCAgent.isOnOffMeshLink:" + _NPCAgent.isOnOffMeshLink);
    }

    static bool recovered = true;
    private float damageTimer = 0.0f;
    private float damageRate = 0.5f;
    private bool dead = false;
    public void OnHit(float amount, Vector3 direction)
    {
        if (damageTimer >= damageRate)
        {
            _health -= amount;
            //if (_health <= 0.0f && !dead)
            //{
            //    dead = true;
            //    StopAllCoroutines();
            //    Mesh mesh = new Mesh();
            //    skinnedMeshRenderer.BakeMesh(mesh);
            //    GameObject o = Instantiate(_ragdoll);

            //    Transform root = transform.GetChild(0);

            //    CopyTransformsRecurse(root, o.transform);
            //    o.transform.localScale = transform.localScale;
            //    o.transform.GetChild(0).GetComponent<Rigidbody>().AddForce(direction * amount, ForceMode.Impulse);
            //    MeshRenderer mr = o.transform.GetComponentInChildren<MeshRenderer>();
            //    MeshFilter mf = o.transform.GetComponentInChildren<MeshFilter>();
            //    if (mr)
            //    {
            //        mr.material = skinnedMeshRenderer.materials[skinnedMeshMaterialIndex];
            //        mf.mesh = mesh;
            //    }
            //    Destroy(gameObject, 0.01f);
            //}
            if (_health <= 0.0f && !dead)
            {
                dead = true;
                StopAllCoroutines();
                GameObject o = Instantiate(_ragdoll);

                Transform root = transform.GetChild(0);

                CopyTransformsRecurse(root, o.transform);
                ParticleSystem ps;
                foreach (Transform child in o.transform)
                {

                    ps = child.GetComponent<ParticleSystem>();
                    if (ps) ps.Play();
                    if(child.childCount > 0)
                    {
                        Rigidbody rb = child.GetChild(0).GetComponent<Rigidbody>();
                        if(rb) rb.AddForce(Random.insideUnitSphere * amount * 0.5f, ForceMode.Impulse);
                    }
                }
                o.transform.localScale = transform.localScale;
                Destroy(gameObject, 0.01f);
            }
            else
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
            }
        }
    }

    static void CopyTransformsRecurse(Transform src, Transform dst)
    {
        dst.position = src.position;
        dst.rotation = src.rotation;

        foreach (Transform child in dst)
        {
            Transform curSrc = src.Find(child.name);
            if (curSrc)
                CopyTransformsRecurse(curSrc, child);
        }
    }

    public void AEOnJumpStart()
    {
        StartCoroutine(Parabola(_NPCAgent, _parabolaJumpHeight, _parabolaJumpDuration, jumpPoint));
    }

    public void AEOnJumpEnd()
    {
        StartCoroutine(IEStopAgent(0.267f));
        _NPCAgent.CompleteOffMeshLink();
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

        Repeater repeater = new Repeater(attackEnemy);

        return repeater;
    }

    struct ParabolaDebugInfo {
        List<Vector3> p;
    }

    static bool finishedTranslation = true;
    private float jumpTimeNormalized = 0;
    IEnumerator Parabola(NavMeshAgent _NPCAgent, float height, float duration, Vector3 position)
    {
        finishedTranslation = false;
        float maxDistance = 20.0f;

        Vector3 startPos = transform.position;
        Vector3 randomPoint = Random.insideUnitSphere * maxDistance;
        randomPoint.y = 0;
        NavMeshHit hit;
        Vector3 endPos = (position);
        //duration -= 1f;
        if (NavMesh.SamplePosition(endPos, out hit, maxDistance, -1))
        {
            jumpTimeNormalized = 0.0f;
            while (jumpTimeNormalized < 1.0f)
            {
                float yOffset = height * (jumpTimeNormalized - jumpTimeNormalized * jumpTimeNormalized);
                _NPCAgent.transform.position = Vector3.Lerp(startPos, hit.position, jumpTimeNormalized) + yOffset * Vector3.up;
                jumpTimeNormalized += Time.deltaTime / duration;
                _animator.SetFloat("JumpTimeNormalized", jumpTimeNormalized);
                _NPCAgent.destination = endPos;
                _NPCAgent.transform.forward = Vector3.Normalize(endPos - startPos);
                yield return null;
            }
            finishedTranslation = true;
        }
    }

    public float DistanceTo(Vector3 target)
    {
        float result = Vector3.Distance(transform.position, target);
        return result;
    }

    public void MoveForward(bool state)
    {
        if (state)
        {
            ///if(!_NPCAgent.hasPath)
            _NPCAgent.SetDestination(behaviourState.moveTarget);
        }
    }
    float jumpTimer = 0;
    float jumpRate = 2.5f;
    private Vector3 jumpPoint;
    public bool Jump(Vector3 point)
    {
        if (finishedTranslation && jumpTimer >= jumpRate)
        {
            jumpPoint = point;
            StartCoroutine(IEStopAgent(0.533f));
            _animator.SetTrigger("Jump");
            jumpTimer = 0f;
            return true;
        }
        else return false;
    }
    public void MoveForwardDiffuseMap(bool state)
    {
        if (state)
        {
            if (_diffuseNavMap)
            {
                Vector2Int index = _diffuseNavMap.ConvertPositionToIndex(transform.position);
                float d = Vector3.Distance(transform.position, behaviourState.enemy.transform.position);
                //Vector3 dir = _diffuseNavMap.getHighestNodeDirection(index);
                if (finishedTranslation)
                {
                    if (d < 20f)
                    {
                        if (_NPCAgent.isOnOffMeshLink && Jump(_NPCAgent.currentOffMeshLinkData.endPos))
                        {
                        }
                        else
                        {
                            behaviourState.moveTarget = playerLogic.transform.position;
                            _NPCAgent.SetDestination(behaviourState.moveTarget);
                        }
                    }
                    else
                    {
                        behaviourState.moveTarget = _diffuseNavMap.getHighestNodePosition(index.x, index.y);
                        _NPCAgent.SetDestination(behaviourState.moveTarget);
                    }
                }
            }
        }
    }

    public IEnumerator IEStopAgent(float duration)
    {
        float t = 0.0f;
        while (t <= duration)
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
    private float lastHitTime;
    private float nowHitTime;
    private float lastHPAmount;
    private float nowHPAmout;

    Vector3 coverPoint;
    Vector3 eyeSightOffsetY = new Vector3(0.0f, 0.25f, 0.0f);
    private float coverLookUpRange = 50.0f;

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
}


