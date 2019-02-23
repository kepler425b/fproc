using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(Animator))]

public class IKAimTowards : MonoBehaviour
{

    protected Animator animator;

    public bool ikActive = false;
    public Transform targetObj = null;
    Transform chest = null;
    Transform spine = null;
    Transform offset;

    void Awake()
    {
        animator = GetComponent<Animator>();
        chest = animator.GetBoneTransform(HumanBodyBones.Chest);
    }

    void LateUpdate()
    {
        chest.transform.LookAt(targetObj);
        chest.transform.Rotate(Vector3.up, -90.0f);
    }
}