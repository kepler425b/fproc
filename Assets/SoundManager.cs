using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _step0;
    [SerializeField] AudioClip _step1;
    [SerializeField] AudioClip _step2;
    [SerializeField] AudioClip _squeak0;
    [SerializeField] AudioClip _squeak1;

    void RandomizeClips(bool randomizeVol = false, params AudioClip[] clips)
    {
        int index = Random.Range(0, clips.Length);
        float pitch = Random.Range(0.95f, 1.05f);
        if (randomizeVol)
        {
            float volScale = Random.Range(0.5f, 1.0f);
            _audioSource.volume = volScale;
        }
        _audioSource.pitch = pitch;
        _audioSource.clip = clips[index];
        _audioSource.Play();
    }

    public void Step()
    {
        RandomizeClips(false, _step0);
    }

    public void Hit()
    {
        RandomizeClips(_squeak0, _squeak1);
    }
}
