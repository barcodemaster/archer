using System.Collections;
using UnityEngine;

/// <summary>
/// 히트 이펙트 프리팹에 부착. duration 후 오브젝트 풀로 자동 반환.
/// </summary>
public class HitEffect : MonoBehaviour
{
	[SerializeField] private float _duration = 1f;

	private void OnEnable()
	{
		StartCoroutine(ReturnAfterDelay());
	}

	private IEnumerator ReturnAfterDelay()
	{
		yield return new WaitForSeconds(_duration);
		ObjectPool.Instance.Return(gameObject);
	}
}
