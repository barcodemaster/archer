using UnityEngine;

public class RootMotionState : StateMachineBehaviour
{
	public bool useRootMotion = true;

	public override void OnStateEnter(
		Animator animator,
		AnimatorStateInfo stateInfo,
		int layerIndex)
	{
		animator.applyRootMotion = useRootMotion;
	}

	public override void OnStateExit(
		Animator animator,
		AnimatorStateInfo stateInfo,
		int layerIndex)
	{
		animator.applyRootMotion = false;
	}
}
