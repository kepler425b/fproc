using UnityEngine;
using System.Collections;
using System;

public class AttackEnemy : Leaf
{
    float _originalRadius;
    public override NodeStatus OnBehave(BehaviourState state)
    {
        Context context = (Context)state;

        if (context.enemy == null)
            return NodeStatus.FAILURE;

        float distance = context.me.DistanceTo(context.enemy.transform.position);

        //if (distance + context.enemy._radius <= 20f)
        //{
        //    context.me.Jump(context.enemy.transform.position);
        //    return NodeStatus.FAILURE;
        //}

        if (distance >= context.enemy._radius)
        {
            context.me._NPCAgent.stoppingDistance = context._originalRadius;
            //context.me._NPCAgent.isStopped = false;
            return NodeStatus.FAILURE;
        }
        else
        {
            context.me._NPCAgent.stoppingDistance = context.enemy._radius;
            Debug.Log("radius: " + context.enemy._radius);
            //context.me._NPCAgent.isStopped = true;
            context.me.Attack();
            context.me.transform.LookAt(context.moveTarget);
            return NodeStatus.SUCCESS;
        }
    }

    public override void OnReset()
    {
    }
}
