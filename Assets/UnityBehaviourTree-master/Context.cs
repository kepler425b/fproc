using UnityEngine;
using System.Collections;

public class Context : BehaviourState
{
    public float _originalRadius;
    public bool withinAttackRadius;
    public Vector3 enemyDirection;
    public AlienLogic me;
    public PlayerLogic enemy;
    public Vector3 moveTarget;
    public Vector3 offsetFromOther;
}

public struct NavAgentStatus {
    public bool isStopped;
    public bool isInUse;
}

