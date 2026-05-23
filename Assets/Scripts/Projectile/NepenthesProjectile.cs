using UnityEngine;

public class NepenthesProjectile : MonoBehaviour
{
	[SerializeField] private float _arcHeight = 3f;
	[SerializeField] private float _duration = 0.8f;

	private float _damage;
	private Vector3 _startPos;
	private Vector3 _targetPos;
	private float _elapsed;

	/// <summary>
	/// 데미지와 목표 위치를 설정한다.
	/// </summary>
	public void Init(float damage, Vector3 targetPos)
	{
		_damage = damage;
		_startPos = transform.position;
		_targetPos = targetPos;
	}

	private void Update()
	{
		_elapsed += Time.deltaTime;
		float t = _elapsed / _duration;

		if (t >= 1f)
		{
			transform.position = _targetPos;
			Destroy(gameObject);
			return;
		}

		Vector3 pos = Vector3.Lerp(_startPos, _targetPos, t);
		pos.y += _arcHeight * 4f * t * (1f - t);
		transform.position = pos;
	}

	private void OnTriggerEnter(Collider other)
	{
		PlayerController player = other.GetComponent<PlayerController>();
		if (player != null)
		{
			player.TakeDamage(_damage);
			Destroy(gameObject);
		}
	}
}
