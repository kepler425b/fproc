using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PropaneTank : MonoBehaviour
{
    [SerializeField] public AudioSource _audioSource;
    [SerializeField] AudioClip _audioClip;
    [SerializeField] Transform _parent;
    [SerializeField] static int _numTanks;
    [SerializeField] static int _maxNumTanksGlobal = 64;

    float delayBetweenSpawn = 0.64f;
    float timeElapsed = 0.0f;
    static int numPlayingSound = 0;
    public bool isPlaying = false;

    void Start()
    {
        gameObject.transform.parent = _parent;    
    }

    // Update is called once per frame
    void Update()
    {
        timeElapsed += Time.deltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        float volume = collision.relativeVelocity.x + collision.relativeVelocity.y + collision.relativeVelocity.z / 3.0f;
        volume = 1.0f - (1.0f / volume);
        _audioSource.pitch = Random.Range(0.95f, 1.05f);
        _audioSource.volume = volume;
        _audioSource.Play();
   
#if true
        if (timeElapsed > delayBetweenSpawn && _numTanks <= _maxNumTanksGlobal)
        {
            GameObject t = Instantiate(gameObject);
            t.transform.position = transform.up + Random.insideUnitSphere;
            t.transform.parent = _parent;
            _numTanks++;
            timeElapsed = 0.0f;
        }
#endif
    }
}
