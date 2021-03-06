﻿using System.Collections;
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
    [SerializeField] AudioClip INSoundRicochet;
    [SerializeField] AudioClip INSoundPiston;
    [SerializeField] AudioClip INSoundCock;
    [SerializeField] Transform _camera;
    [SerializeField] Transform projectileOrigin;
    [SerializeField] GameObject projectile;

    CPMovement playerController;
    WorldState worldState;
    Animator animator;
    [SerializeField] bool isSwitching = false;

    private float nextFire = 0.0f;
    private float lastFire = 0.0f;

    public ParticleSystem gunFX;
    public ParticleSystem gunFXSmoke;

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
    [SerializeField] public float viewModelRayMaxDistance = 1.5f;
    [SerializeField] public float viewModelStrechFactor = 1.0f;

    float charge = 0;
    float chargeTimer = 0;
    float chargeDuration = 2.0f;
    float attackAmount = 0;

    Vector3 originPosition;
    Vector3 originScale;
    Quaternion originRotation;

    float t;
    public float lerpTime = 5.0f;
    WorldState WorldState;
    public GameObject _bulletImpactDecal;

    void Start()
    {
        originPosition = transform.localPosition;
        originScale = transform.localScale;
        originRotation = transform.localRotation;
        playerController = FindObjectOfType<CPMovement>();
        worldState = GameObject.FindObjectOfType<WorldState>();
        _audioSource = GetComponentInParent<AudioSource>();
        if (!_audioSource) _audioSource = gameObject.AddComponent<AudioSource>();
        animator = transform.GetComponent<Animator>();
        gunFX = GetComponentInChildren<ParticleSystem>();
    }

    void Update()
    {
        bool good_to_shoot = Time.time >= lastFire + fireRate && !isSwitching;
        //if (Input.GetKey(KeyCode.Mouse1) && good_to_shoot)
        //{
        //    charge += Time.deltaTime / chargeDuration;
        //    animator.SetFloat("Charge", charge);
        //    if (charge >= 1.0f)
        //    {
        //        attackAmount = charge;
        //        charge = 0;
        //        animator.SetTrigger("Fire");
        //        animator.SetFloat("Charge", charge);
        //        Shoot(fireRate);
        //        StartCoroutine(IEFireRecoilShake());
        //        lastFire = Time.time;
        //    }
        //}
        //else if (charge >= 0.5f && !Input.GetKey(KeyCode.Mouse1) && good_to_shoot)
        //{
        //    attackAmount = charge;
        //    charge = 0;
        //    animator.SetFloat("Charge", charge);
        //    animator.SetTrigger("NoChargeFire");
        //    Shoot(fireRate);
        //    StartCoroutine(IEFireRecoilShake());
        //    lastFire = Time.time;
        //}
        if (Input.GetKeyDown(KeyCode.Mouse0) && good_to_shoot && !isSwitching)
        {
            animator.SetTrigger("Fire");
            Shoot(fireRate);
            StartCoroutine(IEFireRecoilShake());
            lastFire = Time.time;
        }

        if (enableShake)
        {
            IEShake();
            //IEStretchModel();
        }
    }

    RaycastHit IERayHit = new RaycastHit();
    void IEStretchModel()
    {
        transform.localScale = originScale;
        Ray IERay = new Ray(transform.position - transform.forward * 0.5f, transform.forward);
        if(Physics.Raycast(IERay, out IERayHit, viewModelRayMaxDistance))
        {
            Debug.DrawLine(IERay.origin, IERayHit.point, Color.yellow);
            if (viewModelRayMaxDistance > 1f)
            {
                float t = 1f - (IERayHit.distance / 1f);
                Debug.Log(t);
                transform.localScale = new Vector3(originScale.x, originScale.y, originScale.z - t * viewModelStrechFactor);
                float c = Mathf.Lerp(transform.position.z, transform.position.z - t, 0.5f);
                transform.position = new Vector3(transform.position.x, transform.position.y, c);
            }
            else
            {
                transform.localScale = new Vector3(originScale.x, originScale.y, originScale.z * IERayHit.distance);
                transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z - (1f - IERayHit.distance));
            }
        }
        else
        {
            Debug.DrawLine(IERay.origin, IERay.origin + IERay.direction * viewModelRayMaxDistance, Color.yellow);
        }
    }

    void IEShake()
    {
        float playerRotDeltaY = playerController.controllerInfo.rotationDelta.y;
        rotAmountY = -playerRotDeltaY;
        rotAmountZ = playerRotDeltaY;
        float lerpedPosX = Mathf.Lerp(originPosition.x, originPosition.x * playerRotDeltaY * shakeStrengthPosX * Mathf.PerlinNoise(Random.Range(0, 1), Random.Range(0, 1)), _posXLerpStep);
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

    public void SoundCock()
    {
        _audioSource.panStereo = 0f;
        _audioSource.volume = 1.0f;
        _audioSource.pitch = Random.Range(0.95f, 1.05f);
        _audioSource.PlayOneShot(INSoundCock);
    }
   
    void Shoot(float fireRate)
    {
        if (ammo <= 0)
        {
            _audioSource.PlayOneShot(INSoundDry);
            return;
        }

        _audioSource.panStereo = 0f;
        _audioSource.volume = 1.0f;
        _audioSource.pitch = Random.Range(0.9f, 1f);
        _audioSource.PlayOneShot(INSoundFire);

        //_audioSource.volume = Random.Range(0.5f, 0.8f); ;
        //_audioSource.pitch = Random.Range(0.9f, 1.1f);
        //_audioSource.PlayOneShot(INSoundRicochet);

        if (gunFX) gunFX.Play();
        if(gunFXSmoke) gunFXSmoke.Play();

        GameObject b = Instantiate(projectile);
        b.GetComponent<CannonBall>().direction = _camera.transform.forward;
        b.transform.position = projectileOrigin.transform.position;
        b.GetComponent<Rigidbody>().AddForce(projectileOrigin.transform.transform.forward * firePower, ForceMode.Force);
        Object.Destroy(b, 30.0f);

        fireRecoilFactor = 10.0f;
    }

    void DebugAimingRays()
    {
        RaycastHit hit;
        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);
        if (Physics.Raycast(ray, out hit, fireDistance))
        {
            Debug.DrawLine(projectileOrigin.position, hit.point, Color.green);
        }
        else
        {
            Debug.DrawLine(_camera.transform.position, _camera.transform.position + _camera.transform.forward * 4.0f, Color.black);
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
