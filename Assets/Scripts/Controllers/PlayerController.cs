using UnityEngine;
using static Define;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class PlayerController : MonoBehaviour
{
	[SerializeField, Range(1, 5)]
	private float _moveSpeed = 3;

	[SerializeField]
	private float _rotateSpeed = 360;

	[Header("Combat")]
	[SerializeField] private float _maxHp = 1000f;
	[SerializeField] private float _attackAnimSpeed = 1f;
	[SerializeField, Range(0f, 1f)] private float _attackFireTime = 0.3f;
	[SerializeField] private float _attackDamage = 50f;
	[SerializeField] private GameObject _projectilePrefab;
	[SerializeField] private float _weight = 5f;

	private float _currentHp;
	private MonsterBase _attackTarget;
	private bool _hasFired;
	private int _lastLoopIndex;

	private Animator _animator;
	private CharacterController _controller;
	private AudioSource _audioSource;
	private PlayerHPBar _hpBar;
	private TilePassability _passability;

	public float MaxHp => _maxHp;
	public float CurrentHp => _currentHp;
	public float Weight => _weight;

	private EState _state = EState.None;
	public EState State
	{
		get { return _state; }
		private set
		{
			if (_state == value)
				return;

			_state = value;
			UpdateAnimation();
		}
	}

	public bool IsServing { get; set; } = false;

	private void Awake()
	{
		_animator = GetComponent<Animator>();
		_controller = GetComponent<CharacterController>();
		_audioSource = GetComponent<AudioSource>();
		_passability = GetComponent<TilePassability>();
	}

	private void Start()
	{
		_currentHp = _maxHp;
		_hpBar = GetComponentInChildren<PlayerHPBar>();
		if (_hpBar != null)
			_hpBar.SetHP(_currentHp, _maxHp);
	}

	private void Update()
	{
		if (_state == EState.Die)
			return;

		Vector3 dir = GameManager.Instance.JoystickDir;
		Vector3 moveDir = new Vector3(dir.x, 0, dir.y);
		moveDir = (Quaternion.Euler(0, 45, 0) * moveDir).normalized;

		if (moveDir != Vector3.zero)
		{
			_controller.Move(moveDir * Time.deltaTime * _moveSpeed);
			Quaternion lookRotation = Quaternion.LookRotation(moveDir);
			transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, Time.deltaTime * _rotateSpeed);
			_attackTarget = null;
			State = EState.Move;
		}
		else
		{
			MonsterBase target = FindClosestMonster();
			if (target != null)
			{
				_attackTarget = target;

				// 항상 몬스터 방향으로 회전
				Vector3 lookDir = (target.transform.position - transform.position).normalized;
				lookDir.y = 0;
				if (lookDir != Vector3.zero)
					transform.rotation = Quaternion.LookRotation(lookDir);

				if (_state != EState.Attack)
				{
					// 공격 상태 최초 진입: 즉시 애니메이션 시작
					_hasFired = false;
					_lastLoopIndex = 0;
					_animator.speed = _attackAnimSpeed;
					State = EState.Attack;
				}
				else
				{
					AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
					if (info.shortNameHash == Define.ATTACK)
					{
						int loopIndex = Mathf.FloorToInt(info.normalizedTime);
						float loopProgress = info.normalizedTime % 1f;

						if (loopIndex > _lastLoopIndex)
						{
							_hasFired = false;
							_lastLoopIndex = loopIndex;
						}

						if (!_hasFired && loopProgress >= _attackFireTime)
						{
							_hasFired = true;
							FireProjectile(_attackTarget);
						}
					}
				}
			}
			else
			{
				_attackTarget = null;
				State = EState.Idle;
			}
		}

		transform.position = new Vector3(transform.position.x, 0, transform.position.z);
	}

	/// <summary>
	/// 가장 가까운 몬스터를 찾아 반환한다.
	/// </summary>
	private MonsterBase FindClosestMonster()
	{
		MonsterBase[] monsters = FindObjectsByType<MonsterBase>(FindObjectsSortMode.None);
		MonsterBase closest = null;
		float minDist = float.MaxValue;

		foreach (MonsterBase m in monsters)
		{
			if (m.CurrentHp <= 0)
				continue;

			float dist = Vector3.Distance(transform.position, m.transform.position);
			if (dist < minDist)
			{
				minDist = dist;
				closest = m;
			}
		}

		return closest;
	}

	/// <summary>
	/// 타겟 방향으로 투척물을 발사한다.
	/// </summary>
	private void FireProjectile(MonsterBase target)
	{
		if (_projectilePrefab == null)
			return;

		Vector3 direction = (target.transform.position - transform.position).normalized;

		Vector3 spawnPos = transform.position + direction * 0.5f + Vector3.up;
		GameObject go = Instantiate(_projectilePrefab, spawnPos, Quaternion.LookRotation(direction));

		PlayerProjectile proj = go.GetComponent<PlayerProjectile>();
		if (proj != null)
			proj.Init(_attackDamage);
	}

	/// <summary>
	/// 데미지를 받고 HP가 0 이하이면 사망 처리한다.
	/// </summary>
	public void TakeDamage(float damage)
	{
		if (_state == EState.Die)
			return;

		_currentHp -= damage;

		if (_hpBar != null)
			_hpBar.SetHP(_currentHp, _maxHp);

		if (_currentHp <= 0f)
		{
			_currentHp = 0f;
			State = EState.Die;
			Debug.Log("Player died!");
		}
	}

	/// <summary>
	/// 외부에서 플레이어를 밀 때 사용. CharacterController.Move()로 벽 뚫기 방지.
	/// </summary>
	public void Push(Vector3 displacement)
	{
		if (_state == EState.Die) return;
		_controller.Move(displacement);
	}

	private void OnControllerColliderHit(ControllerColliderHit hit)
	{
		MonsterBase monster = hit.gameObject.GetComponentInParent<MonsterBase>();
		if (monster == null || monster.CurrentHp <= 0) return;
		if (_weight <= monster.Weight) return;
		Rigidbody rb = hit.rigidbody;
		if (rb == null || rb.isKinematic) return;
		Vector3 pushDir = hit.moveDirection;
		pushDir.y = 0;
		rb.AddForce(pushDir.normalized * (_weight - monster.Weight), ForceMode.Impulse);
	}

	private void UpdateAnimation()
	{
		switch (State)
		{
			case EState.Idle:
				_animator.speed = 1f;
				_animator.CrossFade(IsServing ? Define.SERVING_IDLE : Define.IDLE, 0.1f);
				break;
			case EState.Move:
				_animator.speed = 1f;
				_animator.CrossFade(IsServing ? Define.SERVING_MOVE : Define.MOVE, 0.05f);
				break;
			case EState.Attack:
				_animator.CrossFade(Define.ATTACK, 0.1f, 0, 0f);
				break;
			case EState.Die:
				_animator.CrossFade(Define.DIE, 0.1f);
				break;
		}
	}
}
