using UnityEngine;

public class MonsterProjectile : MonoBehaviour
{
	[SerializeField] private float _speed = 8f;
	[SerializeField] private float _lifeTime = 3f;

	private float _damage;

	/// <summary>
	/// 데미지 값을 외부에서 설정한다.
	/// </summary>
	public void Init(float damage)
	{
		_damage = damage;
	}

	private void Start()
	{
		Destroy(gameObject, _lifeTime);
	}

	private void Update()
	{
		transform.position += transform.forward * _speed * Time.deltaTime;
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
