using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using UnityEngine.Networking;


public class GunCannon : MonoBehaviour {

    [SerializeField] float firePower = 50000f;
    [SerializeField] float fireDistance = 2000.0f;
    [SerializeField] float fireRate = 0.5f;
    [SerializeField] uint ammo = 100;

    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip INSoundDry;
    [SerializeField] AudioClip INSoundFire;
    [SerializeField] AudioClip INSoundSpring;
    [SerializeField] AudioClip INSoundPiston;
    [SerializeField] Transform _camera;
    [SerializeField] Transform _projectileOrigin;

    CPMovement playerController;
    WorldState worldState;
    Animator animator;
    [SerializeField] bool isSwitching = false;

    private float nextFire = 0.0f;
    private float lastFire = 0.0f;

    public ParticleSystem gunFX;
    public ParticleSystem emptyShellFX;

    [SerializeField] bool  enableShake = true;
    [SerializeField] float shakeStrength = .1f;
    [SerializeField] float shakeDecay = 0.005f;
    [SerializeField] float rotationShakeFactor = .02f;
    [SerializeField] float shakeStrengthPosX = 0.5f;
    [SerializeField] float shakeStrengthPosZ = 1.0f;
    [SerializeField] float shakeStrengthZ = 0.025f;
    [SerializeField] float rotAmountZ, rotAmountY, TranslateAmountZ;
    [SerializeField] public float _posXLerpStep = 0.005f;
    [SerializeField] public float _posZLerpStep = 0.005f;
    [SerializeField] public float fireRecoilFactor = 1.0f;
    [SerializeField] public float _rotZLerpStep = 0.02f;
    [SerializeField] public float _rotYLerpStep = 0.05f;


    float charge = 0;
    float chargeTimer = 0;
    float chargeDuration = 2.0f;
    float attackAmount = 0;

    Vector3 originPosition;
    Quaternion originRotation;

    float t;
    public float lerpTime = 5.0f;
    WorldState WorldState;
    public GameObject _bulletImpactDecal;

    void Start()
    {
        originPosition = transform.localPosition;
        originRotation = transform.localRotation;
        playerController = FindObjectOfType<CPMovement>();
        worldState = GameObject.FindObjectOfType<WorldState>();
        _audioSource = GetComponentInParent<AudioSource>();
        if (!_audioSource) _audioSource = gameObject.AddComponent<AudioSource>();
        animator = transform.GetComponent<Animator>();
    }

    void Update()
    {

        DebugAimingRays();
        RaycastHit hit;

        if (Input.GetKey(KeyCode.Mouse0))
        {
            charge += Time.deltaTime / chargeDuration;
            animator.SetFloat("Charge", charge);
            if (charge >= 1.0f)
            {
                attackAmount = charge;
                charge = 0;
                animator.SetFloat("Charge", charge);
                Shoot(fireRate);
                StartCoroutine(IEFireRecoilShake());
            }
        }
        else if (charge >= 0.01f)
        {
            attackAmount = charge;
            charge = 0;
            animator.SetFloat("Charge", charge);
            Shoot(fireRate);
            StartCoroutine(IEFireRecoilShake());
        }

        if (enableShake)
        {
            StartCoroutine(IEShake());
        }
    }

    IEnumerator IEShake()
    {
        float playerRotDeltaY = playerController.controllerInfo.rotationDelta.y;
        rotAmountY = -playerRotDeltaY;
        rotAmountZ = playerRotDeltaY;
        float lerpedPosX = Mathf.Lerp(originPosition.x, originPosition.x * playerRotDeltaY * shakeStrengthPosX, _posXLerpStep);
        transform.localPosition = new Vector3(lerpedPosX, originPosition.y, originPosition.z);

        float vn = playerController.controllerInfo.velocity == 0f ? 0f : (playerController.controllerInfo.velocity / playerController.controllerInfo.maxVelocity);
        float finalY = rotAmountY * shakeStrength + (playerController.controllerInfo.timeBetweenStepsNormalized * vn) * shakeStrength;
        float clampedY = Mathf.Clamp(finalY, -0.5f, 0.5f);
        transform.localRotation = Quaternion.Slerp(transform.localRotation,
            new Quaternion(originRotation.x,
            clampedY,
            originRotation.z,
            originRotation.w),
            _rotYLerpStep);

        transform.localRotation = Quaternion.Slerp(
        transform.localRotation,
        new Quaternion(originRotation.x,
        originRotation.y,
        (rotAmountZ + playerController.controllerInfo.timeBetweenStepsNormalized * 0.1f) * shakeStrengthZ,
        originRotation.w),
        _rotZLerpStep);

        yield return null;
    }

    IEnumerator LOL()
    {
        while(charge >= 0.0f)
        {
            _audioSource.pitch = 1 - (charge);
            _audioSource.time = charge;
            yield return null;
        }
    }

    IEnumerator IEFireRecoilShake()
    {
        float t = 0;
        while (t <= 1.0f)
        {
            float lerpedPosZ = Mathf.Lerp(originPosition.z * shakeStrengthPosZ, originPosition.z, (1f/(1f-t + (t + 0.5f * 0.5f))));
            transform.localPosition = new Vector3(originPosition.x, originPosition.y, lerpedPosZ);
            t += Time.deltaTime / 0.1f;
            yield return null;
        }
    }

    public void SoundFire()
    {
        StartCoroutine(LOL());
        _audioSource.panStereo = 0f;
        _audioSource.volume = 1.0f;
        _audioSource.pitch = Random.Range(0.9f, 1f);
        _audioSource.PlayOneShot(INSoundFire);
    }
    public void SoundSpring()
    {
        _audioSource.pitch = Random.Range(0.8f, 1f);
        _audioSource.PlayOneShot(INSoundSpring, Random.Range(0.1f, 0.4f));
    }
    public void SoundPiston()
    {
        _audioSource.panStereo = Random.Range(-0.5f, 0f);
        _audioSource.pitch = Random.Range(0.9f, 1f);
        _audioSource.PlayOneShot(INSoundPiston, Random.Range(0.1f, 0.3f));
    }

    void Shoot(float fireRate)
    {
        if (ammo <= 0)
        {
            _audioSource.PlayOneShot(INSoundDry);
            return;
        }
        animator.SetTrigger("Fire");
        fireRecoilFactor = 10.0f;
        RaycastHit hit;
        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);
        if (Physics.Raycast(ray, out hit, fireDistance) && Time.time > lastFire + fireRate && !isSwitching)
        {
            fireRate = animator.GetCurrentAnimatorClipInfo(0).Length;
            Vector3 recoil = new Vector3(Random.Range(-0.05f, 0.05f), Random.Range(-0.05f, 0.05f), Random.Range(-0.05f, 0.05f));
            Vector3 dir = (hit.point + recoil) - _projectileOrigin.position;
           
            ammo--;
            lastFire = Time.time;
            if (emptyShellFX) emptyShellFX.Play();
            if (hit.collider.gameObject.tag == "Enemy")
            {
                hit.collider.gameObject.GetComponent<AlienLogic>().OnHit(10f);
            }
            if(hit.collider.gameObject.tag == "Physics Prop")
            {
                hit.collider.gameObject.GetComponent<Rigidbody>().AddForce(ray.direction * 5.0f, ForceMode.Impulse);
            }
        }
        else if (Time.time > lastFire + fireRate && !isSwitching)
        {
            fireRate = animator.GetCurrentAnimatorClipInfo(0).Length;
            ammo--;
            lastFire = Time.time;
            if(emptyShellFX) emptyShellFX.Play();
        } 
    }

    void DebugAimingRays()
    {
        RaycastHit hit;
        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);
        if (Physics.Raycast(ray, out hit, fireDistance))
        {
            Vector3 dir = hit.point - _projectileOrigin.position;
            Debug.DrawRay(_projectileOrigin.position, dir * hit.distance, Color.green);
        }
    }

    public IEnumerator SwitchingDelay(float s)
	{
		isSwitching = true;
		yield return new WaitForSeconds (s);
		isSwitching = false;
	}

	void OnEnable()
	{
		StartCoroutine(SwitchingDelay (1.2f));
    }
}
