using UnityEngine;

public class ExitDoor : MonoBehaviour
{
	private GameObject _blocker;

	private void Start()
	{
		Transform blockerTransform = transform.Find("Blocker");
		if (blockerTransform != null)
			_blocker = blockerTransform.gameObject;
	}

	/// <summary>
	/// 클리어 시 Blocker를 비활성화하여 문을 열어준다.
	/// </summary>
	public void OpenDoor()
	{
		if (_blocker != null)
			_blocker.SetActive(false);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.GetComponent<PlayerController>() != null)
		{
			StageManager.Instance.GoToNextStage();
		}
	}
}
