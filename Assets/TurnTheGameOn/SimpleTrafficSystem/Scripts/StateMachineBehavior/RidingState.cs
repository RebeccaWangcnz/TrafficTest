using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TurnTheGameOn.SimpleTrafficSystem;
using UnityEngine.AI;
/// <summary>
/// 该状态机行为脚本用于控制动画播放速度和非机动车行驶速度一致
/// </summary>
public class RidingState : StateMachineBehaviour
{
    public float animatorPlaySpeed=1f;
    private NavMeshAgent agent;
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        agent = animator.GetComponent<NavMeshAgent>();
    }


    //OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.speed = animatorPlaySpeed * agent.velocity.magnitude;//保持动画播放速度和AI行驶速度一致
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.speed = 1.0f;//还原动画播放
    }

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}
