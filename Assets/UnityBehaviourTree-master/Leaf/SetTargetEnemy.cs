using UnityEngine;
using System.Collections;
using System;

public class SetTargetEnemy : Leaf {

    public override NodeStatus OnBehave(BehaviourState state)
    {
        Context context = (Context)state;
        //context.moveTarget = context.enemy.transform.position;
        return NodeStatus.SUCCESS;
    }

    public override void OnReset()
    {
    }
}
