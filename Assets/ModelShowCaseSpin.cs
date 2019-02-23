using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelShowCaseSpin : MonoBehaviour
{
    public bool Play = true;
    public float Speed = 0.5f;
    float Angle;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Play)
        {
            Angle = +1.0f * Speed;
            transform.Rotate(0, Angle, 0);
            if(Angle > 360.0f)
            {
                Angle = 0.0f;
            }
        }
    }
}
