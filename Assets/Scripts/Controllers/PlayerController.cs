using System.Collections;
using System.Collections.Generic;
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

	private float _footstepTimer;
	private const float FOOTSTEP_INTERVAL = 0.3f;

	private Animator _animator;
	private CharacterController _controller;
	private AudioSource _audioSource;
	private PlayerHPBar _hpBar;
	private TilePassability _passability;
	private PlayerUpgrade _upgrade;

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
		_upgrade = GetComponent<PlayerUpgrade>();
		if (_upgrade == null)
			_upgrade = gameObject.AddComponent<PlayerUpgrade>();

		if (GetComponent<EquipmentVisual>() == null)
			gameObject.AddComponent<EquipmentVisual>();

		_hpBar = GetComponentInChildren<PlayerHPBar>(true);
		if (_hpBar != null)
			_hpBar.gameObject.SetActive(false);

		Rigidbody rb = GetComponent<Rigidbody>();
		if (rb != null)
		{
			rb.isKinematic = true;
			rb.useGravity = false;
		}
	}

	private void Start()
	{
		_maxHp += EquipmentManager.Instance.GetTotalMaxHpBonus();
		_currentHp = _maxHp;
		if (_hpBar != null)
		{
			_hpBar.gameObject.SetActive(true);
			_hpBar.SetHP(_currentHp, _maxHp);
		}
	}

	private void Update()
	{
		if (GameManager.Instance.IsPaused)
			return;

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

			_footstepTimer -= Time.deltaTime;
			if (_footstepTimer <= 0f)
			{
				AudioManager.Instance?.PlayFootstep();
				_footstepTimer = FOOTSTEP_INTERVAL;
			}
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
					float equipAtkSpd = 1f + EquipmentManager.Instance.GetTotalSubStat(Define.ESubStatType.AttackSpeed) / 100f;
				_animator.speed = _attackAnimSpeed * _upgrade.AttackSpeedMultiplier * equipAtkSpd;
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
	/// 타겟 방향으로 발리를 시작한다. 멀티샷이면 코루틴으로 반복.
	/// </summary>
	private void FireProjectile(MonsterBase target)
	{
		if (_projectilePrefab == null)
			return;

		Vector3 baseDir = target.transform.position - transform.position;
		baseDir.y = 0;
		baseDir.Normalize();

		AudioManager.Instance?.PlayPlayerProjectile();
		SpawnVolley(baseDir);

		if (_upgrade.MultiShotLevel > 0)
			StartCoroutine(MultiShotCoroutine(baseDir));
	}

	private IEnumerator MultiShotCoroutine(Vector3 baseDir)
	{
		for (int i = 0; i < _upgrade.MultiShotLevel; i++)
		{
			yield return new WaitForSeconds(_upgrade.MultiShotDelay);
			if (_state == EState.Die) yield break;
			SpawnVolley(baseDir);
		}
	}

	/// <summary>
	/// 모든 방향에 대해 전방화살 오프셋을 적용하여 투사체를 생성한다.
	/// </summary>
	private void SpawnVolley(Vector3 baseDir)
	{
		List<Vector3> directions = BuildDirections(baseDir);

		float baseDamage = _attackDamage + EquipmentManager.Instance.GetTotalAttackBonus();
		baseDamage *= _upgrade.AttackBoostMultiplier;
		baseDamage *= _upgrade.GiantDamageMultiplier;
		if (_upgrade.HasRage)
		{
			float missingHpPercent = (1f - _currentHp / _maxHp) * 100f;
			baseDamage *= 1f + missingHpPercent * _upgrade.RageAttackPerHpPercent / 100f;
		}

		float equipCritChance = EquipmentManager.Instance.GetTotalSubStat(Define.ESubStatType.CritChance) / 100f;
		float equipCritDamage = EquipmentManager.Instance.GetTotalSubStat(Define.ESubStatType.CritDamage) / 100f;

		ProjectileInitData data = new ProjectileInitData
		{
			damage = baseDamage,
			isPiercing = _upgrade.IsPiercing,
			headshotChance = _upgrade.HeadshotChance,
			wallBounce = _upgrade.IsWallBounce,
			ricochetCount = _upgrade.RicochetCount,
			ricochetRadius = _upgrade.RicochetRadius,
			critChance = _upgrade.CritChance + equipCritChance,
			critDamageMin = _upgrade.CritDamageMin + equipCritDamage,
			critDamageMax = _upgrade.CritDamageMax + equipCritDamage,
		};

		foreach (Vector3 dir in directions)
		{
			int totalCount = 1 + _upgrade.FrontArrowLevel;
			Vector3 perp = Vector3.Cross(Vector3.up, dir).normalized;

			for (int i = 0; i < totalCount; i++)
			{
				float offset = (i - (totalCount - 1) / 2f) * 0.4f;
				Vector3 spawnPos = transform.position + dir * 0.5f + perp * offset;
				spawnPos.y = 1f;

				GameObject go = ObjectPool.Instance.Get(_projectilePrefab);
				go.transform.position = spawnPos;
				go.transform.rotation = Quaternion.LookRotation(dir);
				ProjectileBase proj = go.GetComponent<ProjectileBase>();
				if (proj != null)
					proj.Init(data);
			}
		}
	}

	/// <summary>
	/// 업그레이드에 따라 발사 방향 목록을 생성한다.
	/// </summary>
	private List<Vector3> BuildDirections(Vector3 baseDir)
	{
		List<Vector3> dirs = new List<Vector3> { baseDir };

		if (_upgrade.HasDiagonalArrow)
		{
			dirs.Add(Quaternion.Euler(0, 45, 0) * baseDir);
			dirs.Add(Quaternion.Euler(0, -45, 0) * baseDir);
		}

		if (_upgrade.HasSideArrow)
		{
			dirs.Add(Quaternion.Euler(0, 90, 0) * baseDir);
			dirs.Add(Quaternion.Euler(0, -90, 0) * baseDir);
		}

		if (_upgrade.HasBackArrow)
		{
			dirs.Add(Quaternion.Euler(0, 180, 0) * baseDir);
		}

		return dirs;
	}

	/// <summary>
	/// 데미지를 받고 HP가 0 이하이면 사망 처리한다.
	/// </summary>
	public void TakeDamage(float damage)
	{
		if (_state == EState.Die)
			return;

		float resistance = EquipmentManager.Instance.GetTotalSubStat(Define.ESubStatType.DamageResistance);
		damage *= (1f - Mathf.Clamp01(resistance / 100f));

		_currentHp -= damage;

		DamageTextSpawner.Spawn(transform.position + Vector3.up * 1.5f, damage, true);

		if (_hpBar != null)
			_hpBar.SetHP(_currentHp, _maxHp);

		if (_currentHp <= 0f)
		{
			if (_upgrade.HasExtraLife)
			{
				_upgrade.ConsumeExtraLife();
				_currentHp = _maxHp;
				if (_hpBar != null)
					_hpBar.SetHP(_currentHp, _maxHp);
				StartCoroutine(ReviveSequence());
				return;
			}

			_currentHp = 0f;
			State = EState.Die;
		}
	}

	/// <summary>
	/// HP를 회복한다. 최대 HP를 넘기지 않는다.
	/// </summary>
	public void Heal(float amount)
	{
		if (_state == EState.Die) return;
		_currentHp = Mathf.Min(_currentHp + amount, _maxHp);
		if (_hpBar != null)
			_hpBar.SetHP(_currentHp, _maxHp);
	}

	/// <summary>
	/// 최대 HP를 증가시키고 일정량 즉시 회복한다.
	/// </summary>
	public void BoostHp(float maxHpPercent, float healPercent)
	{
		float addMax = _maxHp * maxHpPercent;
		_maxHp += addMax;
		_currentHp = Mathf.Min(_currentHp + addMax * healPercent, _maxHp);
		if (_hpBar != null)
			_hpBar.SetHP(_currentHp, _maxHp);
	}

	/// <summary>
	/// 능력 선택 시 즉시 효과를 적용한다.
	/// </summary>
	public void ApplyUpgradeEffect(EUpgradeType type)
	{
		switch (type)
		{
			case EUpgradeType.WallPass:
				if (_passability != null)
					_passability.AddFlag(ETilePassFlag.WallPass);
				ApplyWallPassCollisions();
				break;
			case EUpgradeType.WaterWalker:
				if (_passability != null)
					_passability.AddFlag(ETilePassFlag.WaterWalk);
				ApplyWaterWalkCollisions();
				break;
			case EUpgradeType.Dwarf:
				var dwarfInfo = UpgradeDatabase.GetInfo(EUpgradeType.Dwarf);
				transform.localScale *= dwarfInfo?.scale ?? 0.9f;
				break;
			case EUpgradeType.Giant:
				var giantInfo = UpgradeDatabase.GetInfo(EUpgradeType.Giant);
				transform.localScale *= giantInfo?.scale ?? 1.1f;
				break;
			case EUpgradeType.HpBoost:
				BoostHp(_upgrade.HpBoostPercent, _upgrade.HpBoostHealPercent);
				break;
			case EUpgradeType.FastGrowth:
				ExpManager.Instance.SetMaxLevel(_upgrade.MaxPlayerLevel);
				break;
		}
	}

	/// <summary>
	/// 비경계 BlockObstacle의 콜라이더를 트리거로 전환하여 WallPass 통과를 허용한다.
	/// </summary>
	public void ApplyWallPassCollisions()
	{
		BlockObstacle[] blocks = FindObjectsByType<BlockObstacle>(FindObjectsSortMode.None);
		foreach (BlockObstacle block in blocks)
		{
			if (block.IsBoundary) continue;
			Collider col = block.GetComponent<Collider>();
			if (col != null)
				col.isTrigger = true;
		}
	}

	/// <summary>
	/// WaterObstacle의 콜라이더를 트리거로 전환하여 WaterWalker 통과를 허용한다.
	/// </summary>
	public void ApplyWaterWalkCollisions()
	{
		WaterObstacle[] waters = FindObjectsByType<WaterObstacle>(FindObjectsSortMode.None);
		foreach (WaterObstacle water in waters)
		{
			Collider col = water.GetComponent<Collider>();
			if (col != null)
				col.isTrigger = true;
		}
	}

	/// <summary>
	/// Extra Life 부활 연출. 슬로우모션 3초 후 정상 속도로 복귀.
	/// </summary>
	private IEnumerator ReviveSequence()
	{
		Time.timeScale = 0.3f;
		yield return new WaitForSecondsRealtime(3f);
		Time.timeScale = 1f;
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

	/// <summary>
	/// 장비 변경 시 최대 HP를 재계산하고 비례 HP를 조정한다.
	/// </summary>
	public void RecalculateMaxHp()
	{
		float ratio = _maxHp > 0 ? _currentHp / _maxHp : 1f;
		float baseMax = _maxHp - EquipmentManager.Instance.GetTotalMaxHpBonus();
		_maxHp = baseMax + EquipmentManager.Instance.GetTotalMaxHpBonus();
		_currentHp = _maxHp * ratio;
		if (_hpBar != null)
			_hpBar.SetHP(_currentHp, _maxHp);
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
