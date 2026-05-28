using System.Collections.Generic;
using UnityEngine;
using static Define;

public abstract class MonsterBase : MonoBehaviour
{
	[SerializeField] private float _maxHp = 100f;
	[SerializeField] private float _damage = 10f;
	[SerializeField] private float _contactDamageCooldown = 0.5f;
	[SerializeField] private Vector3 _snapToGroundOffset;
	[SerializeField] private Vector3 _triggerColliderCenter;

	[SerializeField] private float _physicsRadius = 0.35f;
	[SerializeField] private float _physicsHeight = 1.6f;
	[SerializeField] private float _weight = 1f;
	[SerializeField] protected bool _overlapWithPlayer = false;
	[SerializeField] protected bool _immovable = false;
	[SerializeField] private bool _isBoss = false;
	[SerializeField] private int _expReward = 20;

	private float _currentHp;
	private float _contactDamageTimer;
	private Transform _target;
	private Animator _animator;
	private Collider _collider;
	private Collider _physicsCollider;
	private Rigidbody _rb;
	private EState _state = EState.Idle;
	private MonsterHPBar _hpBar;
	private TilePassability _passability;

	// 경로 추종
	private List<Vector2Int> _path = new();
	private int _pathIndex;
	private float _pathRefreshTimer;
	private const float PATH_REFRESH_INTERVAL = 0.5f;

	public EState State
	{
		get => _state;
		protected set
		{
			if (_state == value)
				return;
			_state = value;
			UpdateAnimation();
		}
	}

	public float MaxHp => _maxHp;
	public float CurrentHp => _currentHp;
	public float Damage => _damage;
	public float Weight => _weight;
	public bool IsBoss => _isBoss;
	public int ExpReward => _expReward;
	public bool OverlapWithPlayer => _overlapWithPlayer;
	protected Transform Target => _target;
	protected Animator Anim => _animator;
	protected TilePassability Passability => _passability;
	protected Rigidbody Rb => _rb;

	protected virtual void Start()
	{
		_currentHp = _maxHp;
		_animator = GetComponent<Animator>();
		_collider = GetComponent<Collider>();
		if (_overlapWithPlayer && _collider is CapsuleCollider triggerCapsule)
			triggerCapsule.center = _triggerColliderCenter;
		_hpBar = GetComponentInChildren<MonsterHPBar>();
		_passability = GetComponent<TilePassability>();

		// 비겹침 몬스터만 물리 콜라이더 + Rigidbody 추가
		if (!_overlapWithPlayer)
		{
			CapsuleCollider physCapsule = GetComponent<CapsuleCollider>();
			if (physCapsule == null || physCapsule.isTrigger)
				physCapsule = gameObject.AddComponent<CapsuleCollider>();
			physCapsule.isTrigger = false;
			physCapsule.radius = _physicsRadius;
			physCapsule.height = _physicsHeight;
			physCapsule.center = new Vector3(0, _physicsHeight / 2f, 0);
			_physicsCollider = physCapsule;

			Rigidbody rb = GetComponent<Rigidbody>();
			if (rb == null)
				rb = gameObject.AddComponent<Rigidbody>();
			rb.useGravity = false;
			rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
			rb.mass = _weight;
			_rb = rb;

			if (_immovable)
				_rb.constraints = RigidbodyConstraints.FreezeAll;
		}

		if (_animator != null)
			_animator.applyRootMotion = false;

		if (_hpBar != null)
			_hpBar.SetHP(_currentHp, _maxHp);

		transform.position = new Vector3(transform.position.x, _snapToGroundOffset.y, transform.position.z);

		PlayerController player = FindAnyObjectByType<PlayerController>();
		if (player != null)
			_target = player.transform;
	}

	protected virtual void Update()
	{
		if (_state == EState.Die)
			return;

		if (_target == null)
		{
			PlayerController player = FindAnyObjectByType<PlayerController>();
			if (player != null)
				_target = player.transform;
			else
				return;
		}

	}

	/// <summary>
	/// 데미지를 받고 HP가 0 이하이면 사망 처리한다.
	/// </summary>
	public void TakeDamage(float damage, bool isCritical = false)
	{
		if (_state == EState.Die)
			return;

		_currentHp -= damage;

		AudioManager.Instance?.PlayHit();
		CameraController.Instance?.Shake(0.15f, 0.15f);
		if (isCritical)
			DamageTextSpawner.SpawnCritical(transform.position + Vector3.up, damage);
		else
			DamageTextSpawner.Spawn(transform.position + Vector3.up, damage, false);

		if (_hpBar != null)
			_hpBar.SetHP(_currentHp, _maxHp);

		if (_currentHp <= 0f)
		{
			_currentHp = 0f;
			Die();
		}
	}

	/// <summary>
	/// HP를 0으로 만들고 즉사시킨다. 보스에게는 사용 불가.
	/// </summary>
	public void InstantKill()
	{
		if (_state == EState.Die) return;

		_currentHp = 0f;

		AudioManager.Instance?.PlayHit();
		CameraController.Instance?.Shake(0.15f, 0.15f);

		if (_hpBar != null)
			_hpBar.SetHP(0f, _maxHp);

		Die();
	}

	protected virtual void UpdateAnimation()
	{
		if (_animator == null) return;
		switch (State)
		{
			case EState.Idle:
				_animator.speed = 1f;
				_animator.CrossFade(Define.IDLE, 0.1f);
				break;
			case EState.Move:
				_animator.speed = 1f;
				_animator.CrossFade(Define.MOVE, 0.1f);
				break;
			case EState.Attack:
				_animator.speed = 1f;
				_animator.CrossFade(Define.ATTACK, 0.1f);
				break;
			case EState.Die:
				_animator.speed = 1f;
				_animator.CrossFade(Define.DIE, 0.1f);
				break;
			case EState.Jump:
				_animator.speed = 1f;
				_animator.CrossFade(Define.JUMP, 0.1f);
				break;
			case EState.Skill1:
				_animator.speed = 1f;
				_animator.CrossFade(Define.SKILL1, 0.1f);
				break;
			case EState.Skill2:
				_animator.speed = 1f;
				_animator.CrossFade(Define.SKILL2, 0.1f);
				break;
			case EState.Skill3:
				_animator.speed = 1f;
				_animator.CrossFade(Define.SKILL3, 0.1f);
				break;
		}
	}

	private void Die()
	{
		State = EState.Die;
		StopMovement();
		StageManager.Instance.OnMonsterDead(this);
		if (_collider != null) _collider.enabled = false;
		if (_physicsCollider != null) _physicsCollider.enabled = false;
		if (_rb != null) _rb.isKinematic = true;
		OnDie();
		Destroy(gameObject, 1.5f);
	}

	protected virtual void OnDie() { }

	/// <summary>
	/// 목표 위치로 이동 가능한지 타일맵 기준으로 판정한다.
	/// </summary>
	protected bool CanMoveTo(Vector3 worldPos)
	{
		TileMap tileMap = StageManager.Instance.TileMap;
		if (tileMap == null || _passability == null)
			return true;
		return tileMap.CanPass(worldPos, _passability.PassFlags);
	}

	/// <summary>
	/// Rigidbody가 있으면 velocity로, 없으면 transform.position으로 이동.
	/// </summary>
	protected void MoveToward(Vector3 direction, float speed)
	{
		if (_rb != null)
			_rb.linearVelocity = new Vector3(direction.x * speed, 0, direction.z * speed);
		else
			transform.position += direction * speed * Time.deltaTime;
	}

	/// <summary>
	/// A* 경로를 갱신한다. 주기적으로 호출.
	/// </summary>
	protected void RefreshPath()
	{
		_pathRefreshTimer -= Time.deltaTime;
		if (_pathRefreshTimer > 0f && _path.Count > 0)
			return;

		_pathRefreshTimer = PATH_REFRESH_INTERVAL;
		TileMap tileMap = StageManager.Instance.TileMap;
		if (tileMap == null || _passability == null || _target == null)
		{
			_path.Clear();
			return;
		}

		Vector2Int myGrid = tileMap.WorldToGrid(transform.position);
		Vector2Int targetGrid = tileMap.WorldToGrid(_target.position);
		_path = AStarPathfinder.FindPath(tileMap, myGrid, targetGrid, _passability.PassFlags);
		_pathIndex = 0;
	}

	/// <summary>
	/// 현재 경로의 다음 웨이포인트 방향을 반환한다. 경로 없으면 직선 방향 fallback.
	/// </summary>
	protected Vector3 GetPathDirection()
	{
		TileMap tileMap = StageManager.Instance.TileMap;
		if (tileMap == null || _path.Count == 0 || _pathIndex >= _path.Count)
		{
			if (_target == null) return Vector3.zero;
			Vector3 direct = (_target.position - transform.position).normalized;
			direct.y = 0;
			return direct;
		}

		Vector3 waypointWorld = tileMap.GridToWorld(_path[_pathIndex].x, _path[_pathIndex].y);
		Vector3 toWaypoint = waypointWorld - transform.position;
		toWaypoint.y = 0;

		if (toWaypoint.magnitude < 0.3f)
		{
			_pathIndex++;
			if (_pathIndex >= _path.Count)
			{
				if (_target == null) return Vector3.zero;
				Vector3 direct = (_target.position - transform.position).normalized;
				direct.y = 0;
				return direct;
			}
			waypointWorld = tileMap.GridToWorld(_path[_pathIndex].x, _path[_pathIndex].y);
			toWaypoint = waypointWorld - transform.position;
			toWaypoint.y = 0;
		}

		return toWaypoint.normalized;
	}

	/// <summary>
	/// 투사체 피격 시 방향으로 넉백시킨다. Rigidbody가 없으면 무시.
	/// </summary>
	public void Knockback(Vector3 direction, float force)
	{
		if (_rb == null) return;
		direction.y = 0;
		_rb.AddForce(direction.normalized * force, ForceMode.Impulse);
	}

	protected void StopMovement()
	{
		if (_rb != null)
			_rb.linearVelocity = Vector3.zero;
	}

	private void OnCollisionStay(Collision collision)
	{
		if (_state == EState.Die) return;
		PlayerController player = collision.gameObject.GetComponent<PlayerController>();
		if (player == null) return;
		if (_weight <= player.Weight) return;
		Vector3 pushDir = (player.transform.position - transform.position).normalized;
		pushDir.y = 0;
		player.Push(pushDir * (_weight - player.Weight) * 0.5f * Time.deltaTime);
	}

	private void OnTriggerStay(Collider other)
	{
		if (_state == EState.Die) return;
		PlayerController player = other.GetComponent<PlayerController>();
		if (player == null) return;
		_contactDamageTimer -= Time.deltaTime;
		if (_contactDamageTimer > 0f) return;
		player.TakeDamage(_damage);
		_contactDamageTimer = _contactDamageCooldown;
	}
}
