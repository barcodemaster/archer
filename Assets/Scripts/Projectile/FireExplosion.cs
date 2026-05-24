using System.Collections;
using UnityEngine;

/// <summary>
/// 파이어볼 폭발: 즉시 범위 데미지 + 잔류 화염 도트 데미지.
/// </summary>
public class FireExplosion : MonoBehaviour
{
	[SerializeField] private float _explosionRadius = 2f;
	[SerializeField] private float _lingerDuration = 2f;
	[SerializeField] private float _lingerDamageInterval = 0.5f;
	[SerializeField] private float _lingerDamageRatio = 0.3f;

	private float _damage;
	private float _lingerDamageTimer;
	private SphereCollider _sphereCollider;

	/// <summary>
	/// 폭발 데미지를 설정한다.
	/// </summary>
	public void Init(float damage)
	{
		_damage = damage;
	}

	private void OnEnable()
	{
		_lingerDamageTimer = 0f;

		if (_sphereCollider == null)
		{
			_sphereCollider = gameObject.AddComponent<SphereCollider>();
			_sphereCollider.isTrigger = true;
			_sphereCollider.radius = _explosionRadius;
		}
		_sphereCollider.enabled = true;

		StartCoroutine(ExplosionRoutine());
	}

	private IEnumerator ExplosionRoutine()
	{
		// 즉시 폭발 데미지
		Collider[] hits = Physics.OverlapSphere(transform.position, _explosionRadius);
		foreach (Collider hit in hits)
		{
			PlayerController player = hit.GetComponent<PlayerController>();
			if (player != null)
			{
				player.TakeDamage(_damage);
				break;
			}
		}

		CameraController.Instance?.Shake(0.2f, 0.25f);

		yield return new WaitForSeconds(_lingerDuration);

		_sphereCollider.enabled = false;
		ObjectPool.Instance.Return(gameObject);
	}

	private void OnTriggerStay(Collider other)
	{
		PlayerController player = other.GetComponent<PlayerController>();
		if (player == null) return;

		_lingerDamageTimer -= Time.deltaTime;
		if (_lingerDamageTimer > 0f) return;

		player.TakeDamage(_damage * _lingerDamageRatio);
		_lingerDamageTimer = _lingerDamageInterval;
	}
}
