using System.Collections;
using UnityEngine;

/// <summary>
/// 천사방에 배치되는 NPC. 플레이어가 접근하면 축복 선택 UI를 연다.
/// </summary>
public class AngelNPC : MonoBehaviour
{
	[Header("Drop Animation")]
	[SerializeField] private float _dropHeight = 8f;
	[SerializeField] private float _dropDuration = 0.6f;

	[Header("Ascend Animation")]
	[SerializeField] private float _ascendHeight = 10f;
	[SerializeField] private float _ascendDuration = 0.8f;

	private bool _hasInteracted = false;

	private void OnTriggerEnter(Collider other)
	{
		if (_hasInteracted) return;

		PlayerController player = other.GetComponent<PlayerController>();
		if (player == null)
			player = other.GetComponentInParent<PlayerController>();
		if (player == null) return;

		_hasInteracted = true;
		UIManager.Instance.ShowAngelPanel(this);
	}

	/// <summary>
	/// 입장 시 위에서 떨어지는 드롭 연출.
	/// </summary>
	public void PlayDropAnimation()
	{
		StartCoroutine(DropCoroutine());
	}

	private IEnumerator DropCoroutine()
	{
		Vector3 targetPos = transform.position;
		Vector3 startPos = targetPos + Vector3.up * _dropHeight;
		transform.position = startPos;

		float elapsed = 0f;
		while (elapsed < _dropDuration)
		{
			elapsed += Time.deltaTime;
			float t = Mathf.Clamp01(elapsed / _dropDuration);
			float eased = t * t; // quadratic ease-in
			transform.position = Vector3.Lerp(startPos, targetPos, eased);
			yield return null;
		}

		transform.position = targetPos;
		AudioManager.Instance.PlayLanding();
	}

	/// <summary>
	/// 선택 후 위로 상승하며 사라지는 연출.
	/// </summary>
	public void PlayAscendAnimation()
	{
		StartCoroutine(AscendCoroutine());
	}

	private IEnumerator AscendCoroutine()
	{
		Vector3 startPos = transform.position;
		Vector3 targetPos = startPos + Vector3.up * _ascendHeight;

		float elapsed = 0f;
		while (elapsed < _ascendDuration)
		{
			elapsed += Time.deltaTime;
			float t = Mathf.Clamp01(elapsed / _ascendDuration);
			float eased = 1f - (1f - t) * (1f - t); // quadratic ease-out
			transform.position = Vector3.Lerp(startPos, targetPos, eased);
			yield return null;
		}

		transform.position = targetPos;
		gameObject.SetActive(false);
	}
}
