using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldState : MonoBehaviour
{
    public Transform ImpactDecals;
    [SerializeField] int _numTanksPlayingSound;

    PropaneTank lastTank = null;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        foreach (PropaneTank tank in GetComponentsInChildren<PropaneTank>())
        {
            if(tank)
            {
                if(tank._audioSource.isPlaying)
                {
                    _numTanksPlayingSound++;
                }
                else
                {
                    _numTanksPlayingSound--;
                }
            }
        }
        if (_numTanksPlayingSound < 0) _numTanksPlayingSound = 0;
    }

    private void LateUpdate()
    {
        _numTanksPlayingSound = 0;
    }
}
