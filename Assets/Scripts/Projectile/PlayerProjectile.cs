using UnityEngine;

public class PlayerProjectile : MonoBehaviour
{
	[SerializeField] private float _speed = 10f;
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
		MonsterBase monster = other.GetComponent<MonsterBase>();
		if (monster != null)
		{
			monster.TakeDamage(_damage);
			Destroy(gameObject);
		}
	}
}
