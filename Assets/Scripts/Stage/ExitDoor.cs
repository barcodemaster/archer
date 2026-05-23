using UnityEngine;

public class ExitDoor : MonoBehaviour
{
	private void OnTriggerEnter(Collider other)
	{
		if (other.GetComponent<PlayerController>() != null)
		{
			StageManager.Instance.GoToNextStage();
		}
	}
}
