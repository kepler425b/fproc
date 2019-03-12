using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;
using UnityEngine.UI;


public class gunModule : MonoBehaviour {

	public enum FireMode {
		single,
		automatic,
		burst
	};
    
	public FireMode fireMode = FireMode.single;
	public Transform _projectileOrigin;
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
    [SerializeField] Transform _camera;
    public Transform weaponLookTarget;
    public Vector3 adjust;
    public uint ammo;
    public uint ammo_capacity;
    bool isBeingDrawn = false;
    CPMovement playerController;
    public ParticleSystem gunFX;
    public ParticleSystem emptyShellFX;

    public bool EnableShake = true;
    public float shakeStrength = .1f;
    public float shakeDecay = 0.005f;
    public float rotationShakeFactor = .02f;
    public float shakeMin = 0.1f;
    public float shakeMax = 0.3f;
    public float rotAmountZ, rotAmountY;
    [SerializeField] public float _posXLerpStep = 0.005f;
    [SerializeField] public float _rotZLerpStep = 0.02f;
    [SerializeField] public float _rotYLerpStep = 0.05f;

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
  
        switch (fireMode)
        {
            case FireMode.single:
                {
                    if (Input.GetKeyDown(KeyCode.Mouse0))
                    {
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
    public void SoundFire()
    {
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
            _audioSource.PlayOneShot(gunDrySound);
            return;
        }
        anim.SetTrigger("Fire");
        RaycastHit hit;
        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);
        if (Physics.Raycast(ray, out hit, fireDistance) && Time.time > lastFire + fireRate && !isSwitching)
        {
            //fireRate = anim.GetCurrentAnimatorClipInfo(0).Length;
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
            //gunFX.Play();
            if (emptyShellFX) emptyShellFX.Play();
            if (hit.collider.gameObject.tag == "Enemy")
            {
                hit.collider.gameObject.GetComponent<AlienLogic>().OnHit(10f, Vector3.Normalize(hit.point - transform.position));
            }
            if(hit.collider.gameObject.tag == "Physics Prop")
            {
                hit.collider.gameObject.GetComponent<Rigidbody>().AddForce(ray.direction * 5.0f, ForceMode.Impulse);
            }
        }
        else if (Time.time > lastFire + fireRate && !isSwitching)
        {
            fireRate = anim.GetCurrentAnimatorClipInfo(0).Length;
          
            ammo--;
            lastFire = Time.time;
            //gunFX.Play();
            if(emptyShellFX) emptyShellFX.Play();
        } 
    }

    void DebugAimingRays()
    {
        RaycastHit hit;
        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);
        if (Physics.Raycast(ray, out hit, fireDistance))
        {
            Debug.DrawLine(_projectileOrigin.position, hit.point, Color.green);
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
		//anim.SetTrigger ("Draw");
		StartCoroutine(SwitchingDelay (1.2f));
        _transform = transform;
    }
}
