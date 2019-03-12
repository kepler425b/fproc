using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CannonBall : MonoBehaviour
{
    public Vector3 direction;
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.collider.gameObject.tag == "Enemy")
        {
            float velocity = Vector3.Magnitude(collision.impulse);
            collision.collider.transform.GetComponentInParent<AlienLogic>().OnHit(velocity, direction);
            if (true) Debug.Log("Hit: " + collision.collider.name + " with relative velocity of " + velocity);
        }
    }
}
