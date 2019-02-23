using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using UnityEngine.Networking;


public class cannonModule : MonoBehaviour {


	public enum FireMode {
		single,
		automatic,
		burst
	};
    
	public FireMode fireMode = FireMode.single;
	public Transform _projectileOrigin;
	public Light light;
    public float fire_power = 50000f;
	public float fireDistance = 2000.0f;
	public float fireRateSingle = 0.667f;
	public float fireRateAutomatic = 0.567f;
	private float nextFire = 0.0f;
	private float lastFire = 0.0f;
	public bool isSwitching = false;
	public AudioSource _audioSource;
    public AudioClip gunDrySound;
    public AudioClip INSoundFire;
    public AudioClip INSoundSpring;
    public AudioClip INSoundPiston;
    public Material DebugMaterial;
    Collider player_collider;
    public Text _textOut;
    public Animator anim;
    public Vector3 adj_pos;
    public Vector3 adj_rot;
    public GameObject iron_ref;
    public GameObject camera_ref;
    public Transform weaponLookTarget;
    public Vector3 adjust;
    public uint ammo;
    public uint ammo_capacity;
    bool isBeingDrawn = false;
    CPMovement playerController;
    public ParticleSystem gunFX;
    public ParticleSystem emptyShellFX;
    //Collider collider_torso_upper;
    //Collider collider_torso_bottom;
    //Collider collider_neck;
    //Collider collider_head;
    //Collider[] collider_legs;
    //Collider[] collider_foots;
    public bool EnableShake = true;
    public float shakeStrength = .1f;
    public float shakeDecay = 0.005f;
    public float rotationShakeFactor = .02f;
    public float shakeMin = 0.1f;
    public float shakeMax = 0.3f;
    public float rotAmountZ, rotAmountY, TranslateAmountZ;
    [SerializeField] public float _posXLerpStep = 0.005f;
    [SerializeField] public float _rotZLerpStep = 0.02f;
    [SerializeField] public float _rotYLerpStep = 0.05f;

    float charge = 0;
    float chargeTimer = 0;
    float chargeDuration = 2.0f;
    float attackAmount = 0;

    Vector3 originPosition;
    Quaternion originRotation;
    Transform _transform;

    public AnimationClip FireClip;
    float t;
    public float lerpTime = 5.0f;
    WorldState WorldState;
    public GameObject _bulletImpactDecal;

    void Start()
    {
        originPosition = _transform.localPosition;
        originRotation = _transform.localRotation;
        ammo_capacity = ammo;
        playerController = FindObjectOfType<CPMovement>();
        WorldState = GameObject.FindObjectOfType<WorldState>();
    }

    void Update()
    {

        DebugAimingRays();
        //fireRateAutomatic = FireClip.length * 1f/3f;
        transform.LookAt(weaponLookTarget);
        RaycastHit hit;
        Ray ray = new Ray(camera_ref.transform.position, camera_ref.transform.forward);
        if (Physics.Raycast(ray, out hit, fireDistance) && hit.transform.tag == "neck" && !isBeingDrawn)
        {
            GameObject temp = Instantiate(GameObject.CreatePrimitive(PrimitiveType.Cube), hit.collider.transform.position,
                hit.collider.transform.rotation);
            temp.GetComponent<Renderer>().material.color = Color.green;
            temp.transform.localScale = transform.localScale;
            isBeingDrawn = true;
        }
        else
        {
            isBeingDrawn = false;
        }

        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            //transform.Rotate(adj_rot);
            transform.Translate(adj_pos);

        }
        if (Input.GetKeyUp(KeyCode.Mouse1))
        {
            //transform.Rotate(-adj_rot);
            transform.Translate(-adj_pos);

        }
        switch (fireMode)
        {
            case FireMode.single:
                {
                    if (Input.GetKey(KeyCode.Mouse0))
                    {
                        charge += Time.deltaTime / chargeDuration;
                        anim.SetFloat("Charge", charge);
                        if(charge >= 1.0f)
                        {
                            attackAmount = charge;
                            charge = 0;
                            anim.SetFloat("Charge", charge);
                            Shoot(fireRateSingle);    
                        }
                    }
                    else if (charge >= 0.01f)
                    {
                        attackAmount = charge;
                        charge = 0;
                        anim.SetFloat("Charge", charge);
                        Shoot(fireRateSingle);
                    }
                }
                break;
            case FireMode.automatic:
                {
                    if (Input.GetKey(KeyCode.Mouse0))
                    {
                        Shoot(fireRateAutomatic);
                    }
                }
                break;
        }

        if (Input.GetKeyDown(KeyCode.R) && ammo < ammo_capacity && !isSwitching)
        {
            anim.SetTrigger("Reload");
            //gunreloadSound.Play();
            ammo = ammo_capacity;

            StartCoroutine(SwitchingDelay(4.417f));

        }
        Debug.DrawRay(_projectileOrigin.position, _projectileOrigin.forward * 10f, Color.blue);
        if (EnableShake)
        {
            float playerRotDeltaY = playerController.controllerInfo.rotationDelta.y;

            float lerpedPosX = Mathf.Lerp(originPosition.x, originPosition.x * playerRotDeltaY * 0.25f, _posXLerpStep);
            transform.localPosition = new Vector3(lerpedPosX, originPosition.y, originPosition.z);
            rotAmountZ = originRotation.x - (originRotation.x * playerRotDeltaY * 1.5f);
            rotAmountY = originRotation.y - (originRotation.y * -playerRotDeltaY * 1.5f);

            //string text = "rotAmountZ = " + originRotation.x.ToString() + "- (+ " + originRotation.x.ToString() + " * " + playerRotDeltaY.ToString() + " * " + 1.5f.ToString();
            //string text = "rotAmountZ = " + originRotation.x.ToString() + "- (+ " + originRotation.x.ToString() + " * " + playerRotDeltaY.ToString() + " * " + 1.5f.ToString();
            //_textOut.text = text;
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                new Quaternion(originRotation.x,
                originRotation.y,
                rotAmountZ + playerController.controllerInfo.timeBetweenStepsNormalized * 0.1f,
                originRotation.w),
                _rotZLerpStep);

            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                new Quaternion(originRotation.x,
                rotAmountY + playerController.controllerInfo.timeBetweenStepsNormalized * 0.05f,
                originRotation.z,
                originRotation.w),
                _rotYLerpStep);
        }
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
        anim.SetTrigger("Fire");
        if (ammo <= 0)
        {
            _audioSource.PlayOneShot(gunDrySound);
            return;
        }
        RaycastHit hit;
        Ray ray = new Ray(camera_ref.transform.position, camera_ref.transform.forward);
        if (Physics.Raycast(ray, out hit, fireDistance) && Time.time > lastFire + fireRate && !isSwitching)
        {
            //anim.SetTrigger("Fire");
            fireRate = anim.GetCurrentAnimatorClipInfo(0).Length;
            Vector3 recoil = new Vector3(Random.Range(-0.05f, 0.05f), Random.Range(-0.05f, 0.05f), Random.Range(-0.05f, 0.05f));
            Vector3 dir = (hit.point + recoil) - _projectileOrigin.position;
            //if(_bulletImpactDecal && hit.collider.gameObject.tag != "Physics Prop")
            //{
            //    GameObject decal = Instantiate(_bulletImpactDecal, hit.point + hit.normal * 0.05f, Quaternion.LookRotation(hit.normal, Vector3.up));
            //    decal.transform.SetParent(hit.collider.gameObject.transform);
            //}
            //_audioSource.PlayOneShot(gunShotSound);
            ammo--;
            lastFire = Time.time;
            gunFX.Play();
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
            //anim.SetTrigger("Fire");
            fireRate = anim.GetCurrentAnimatorClipInfo(0).Length;
         
            ammo--;
            lastFire = Time.time;
            gunFX.Play();
            if(emptyShellFX) emptyShellFX.Play();
        } 
    }

    void DebugAimingRays()
    {
        RaycastHit hit;
        Ray ray = new Ray(camera_ref.transform.position, camera_ref.transform.forward);
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
		//anim.SetTrigger ("Draw");
		StartCoroutine(SwitchingDelay (1.2f));
        _transform = transform;
    }
}
