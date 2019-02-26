using UnityEngine;
using System.Collections;
using System;

public class MoveForward : Leaf
{
    public override NodeStatus OnBehave(BehaviourState state)
    {
        Context context = (Context)state;

        if (context.enemy == null)
            return NodeStatus.FAILURE;

        context.me.MoveForwardDiffuseMap(true);

        return NodeStatus.SUCCESS;
    }

    public override void OnReset()
    {
    }
}
