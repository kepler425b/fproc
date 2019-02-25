using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLogic : MonoBehaviour
{
    CPMovement movementController;
    [SerializeField] public float _radius;
    [SerializeField] int _health = 100;
    [SerializeField] public float _healthDelta;
    [SerializeField] float _maxDamageDelta = 20.0f;
    [SerializeField] float timeBetweenHits;
    [SerializeField] float velocityMagnitude;
    bool wasGrounded;
    [SerializeField] Transform _camera;
    [SerializeField] CanvasRenderer _crScreenOverlay;
    [SerializeField] CanvasRenderer _crHUD;
    [SerializeField] DiffusesNodeMap _DiffuseMapDebugHandle;
    [SerializeField] Text textHP;
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _damage_scream0;
    [SerializeField] AudioClip _damage_scream1;
    [SerializeField] AudioClip _damage_scream2;
    [SerializeField] AudioClip _step0;
    [SerializeField] AudioClip _step1;
    [SerializeField] AudioClip _groundHit;
    [SerializeField] AudioClip _jump;
    

    void RandomizeClipsSimple(params AudioClip[] clips)
    {
        int index = Random.Range(0, clips.Length);
        float pitch = Random.Range(0.95f, 1.05f);
        _audioSource.pitch = pitch;
        _audioSource.clip = clips[index];
        _audioSource.Play();
    }

    void RandomizeClips(float pitch = 1.0f, params AudioClip[] clips)
    {
        int index = Random.Range(0, clips.Length);
        _audioSource.pitch = pitch;
        _audioSource.clip = clips[index];
        _audioSource.Play();
    }

    Vector3 ClampIn2DArray(Vector3 input)
    {;
        Vector3 result = new Vector3();
        result.x = Mathf.Clamp(input.x, _DiffuseMapDebugHandle.AMIN * -10.0f * 0.5f, _DiffuseMapDebugHandle.AMAX * 0.5f);
        result.y = input.y;
        result.z = Mathf.Clamp(input.z, _DiffuseMapDebugHandle.AMIN * -10.0f * 0.5f, _DiffuseMapDebugHandle.AMAX * 0.5f);
        return result;
    }

    Color hitEffectColor;
    float hitColorAlphaFactor;
    float lastHitTime = 0.0f;
    float nowHitTime = 0.0f;
    float lastHPAmount = 0.0f;
    float nowHPAmout = 0.0f;

    void Start()
    {
        hitEffectColor = Color.red;

        if (_crScreenOverlay)
        {
            _crScreenOverlay.gameObject.SetActive(true);
        }
        movementController = GetComponent<CPMovement>();
        _DiffuseMapDebugHandle = FindObjectOfType<DiffusesNodeMap>();
    }

    void OnAwake()
    {
        _crScreenOverlay = GetComponent<CanvasRenderer>();
        if (_crScreenOverlay == null) Debug.LogError("Canvas renderer is not attached.");
    }

    float distanceTravelled = 0;
    void Update()
    {
        if (_crScreenOverlay)
        {
            if (_healthDelta != 0.0f)
            {
                hitColorAlphaFactor = (1.0f / _healthDelta);
            }
            else
            {
                _crScreenOverlay.SetAlpha(0.0f);
            }
        }

        transform.position = ClampIn2DArray(transform.position);

        textHP.text = _health.ToString();

         nowHitTime = Time.time;
        timeBetweenHits = nowHitTime - lastHitTime;
        lastHitTime = nowHitTime;

        nowHPAmout = _health;
        _healthDelta = Mathf.Clamp(Mathf.Abs((lastHPAmount - nowHPAmout) / timeBetweenHits), 0.0f, 32.0f);
        lastHPAmount = nowHPAmout;


        //Sound stuff.

        distanceTravelled += movementController.controllerInfo.distanceTravelled;
        
        if (movementController.controllerInfo.didStep)
        {
            RandomizeClipsSimple(_step0, _step1);
            distanceTravelled = 0f;
        }
        if (movementController.controllerInfo.isGrounded && !movementController.controllerInfo.wasGrounded && velocityMagnitude >= 0.01f)
        {
            RandomizeClipsSimple(_groundHit);
        }
        if (!movementController.controllerInfo.isGrounded && wasGrounded && movementController.controllerInfo.didPressJump)
        {
            StartCoroutine(IEJumpSound(1.87f));
        }
        else if(movementController.controllerInfo.isGrounded && !wasGrounded)
        {
            StopCoroutine("IEJumpSound");
        }
        wasGrounded = movementController.controllerInfo.isGrounded;
    }

    private void LateUpdate()
    {

    }

    float jumpSoundPitch = 0;
    bool finishedJumpSound = true;
    IEnumerator IEJumpSound(float duration)
    {
        RandomizeClipsSimple(_jump);
        finishedJumpSound = false;
        float normalizedTime = 0.0f;
        while (normalizedTime < 1.0f)
        {
            jumpSoundPitch = normalizedTime;
            _audioSource.pitch = 1.0f + movementController.controllerInfo.VelocityDelta.y;
            normalizedTime += Time.deltaTime / duration;
            yield return null;
        }
        finishedJumpSound = true;
    }

    static bool recovered = true;
    IEnumerator HitEffect(float duration)
    {
        float normalizedTime = 0.0f;
        while (normalizedTime < 1.0f)
        {
            hitEffectColor.a = Mathf.Lerp(0, hitColorAlphaFactor * 5.0f, normalizedTime);
            normalizedTime += Time.deltaTime / duration;
            _crScreenOverlay.SetAlpha(hitEffectColor.a);
            yield return null;
        }
    }

    float hitSoundRate = 0.5f;
    float t0;
    public void receiveDamage(int damage)
    {
        _health -= damage;
        StartCoroutine(HitEffect(0.25f));
        if(Time.time >= t0 + hitSoundRate)
        {
            RandomizeClipsSimple(_damage_scream0, _damage_scream1);
            t0 = Time.time;
        }
        if (_health <= 0)
        {
            _health = 100;
        }
    }
}
