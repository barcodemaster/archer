using UnityEngine;

public class Trap_Saw : MonoBehaviour
{
    [SerializeField] private float _damage = 20f;

    private Collider _collider;
	private Animator _animator;


	private void Awake()
	{
		_collider = GetComponentInChildren<Collider>();
		_animator = GetComponent<Animator>();	
	}

	private void Start()
	{
		_animator.CrossFade("Idle", 0.1f);
	}

	private void OnTriggerEnter(Collider other)
	{
		if(other.tag == "Player")
		{
			IDamageable damageable = other.GetComponent<IDamageable>();
			damageable.TakeDamage(_damage);
		}
	}

}
