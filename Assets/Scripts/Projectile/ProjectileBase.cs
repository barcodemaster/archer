using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

/// <summary>
/// 통합 프로젝타일: Inspector에서 이동 타입과 옵션을 설정한다.
/// </summary>
public class ProjectileBase : MonoBehaviour
{
	[Header("Common")]
	[SerializeField] private float _speed = 10f;
	[SerializeField] private float _lifeTime = 3f;
	[SerializeField] private float _knockbackForce = 0f;
	[SerializeField] private bool _isPlayerProjectile;
	[SerializeField] private EProjectileMoveType _moveType = EProjectileMoveType.Straight;

	[Header("Bounce Option")]
	[SerializeField] private bool _bounceEnabled;
	[SerializeField] private int _maxBounces = 5;
	[SerializeField] private float _bounceRadius = 0.2f;

	[Header("Arc Option")]
	[SerializeField] private float _arcHeight = 3f;
	[SerializeField] private float _arcDuration = 0.8f;

	[Header("Spin")]
	[SerializeField] private float _spinSpeed = 720f;

	[Header("Explosion")]
	[SerializeField] private GameObject _explosionPrefab;

	[Header("Hit Effect")]
	[SerializeField] private GameObject _hitEffectPrefab;

	[Header("Sound")]
	[SerializeField] private AudioClip _launchSound;

	private float _damage;
	private bool _hasHit;
	private bool _destroyed;
	private float _timer;
	private float _baseSpeed;

	// Upgrade fields
	private bool _isPiercing;
	private float _headshotChance;
	private int _ricochetCount;
	private float _ricochetRadius;
	private float _critChance;
	private float _critDamageMin;
	private float _critDamageMax;
	private float _playerWeight;
	private HashSet<int> _hitMonsterIds = new();

	// Arc
	private Vector3 _startPos;
	private Vector3 _targetPos;
	public Vector3 TargetPos => _targetPos;
	private float _elapsed;

	// Bounce
	private Vector3 _velocity;
	private int _bounceCount;
	private Vector3 _moveDirection;

	private Coroutine _lifeCoroutine;
	private bool _defaultBounceEnabled;
	private Vector3 _originalScale;

	// SFX 중복 방지용 static 쿨다운
	private static float _lastFireSfxTime;
	private static float _lastHitSfxTime;
	private const float SFX_COOLDOWN = 0.05f;

	private void Awake()
	{
		_defaultBounceEnabled = _bounceEnabled;
		_baseSpeed = _speed;
		_originalScale = transform.localScale;
	}

	/// <summary>
	/// 런타임에 아군/적군 발사체 여부를 설정한다. (펫 발사체 등)
	/// </summary>
	public void SetPlayerProjectile(bool value)
	{
		_isPlayerProjectile = value;
	}

	/// <summary>
	/// Straight/Piercing용 초기화.
	/// </summary>
	public void Init(float damage)
	{
		_damage = damage;
		_moveDirection = transform.forward;
		if (_bounceEnabled)
			_velocity = transform.forward * _speed;
	}

	public void Init(float damage, int maxBounce, float speed, float lifeTime)
	{
		_damage = damage;
		_moveDirection = transform.forward;
		_speed = speed;
		_lifeTime = lifeTime;
		_maxBounces = maxBounce;

		if (_bounceEnabled)
			_velocity = transform.forward * _speed;		
	}

	/// <summary>
	/// 업그레이드 데이터로 초기화.
	/// </summary>
	public void Init(ProjectileInitData data)
	{
		_damage = data.damage;
		_isPiercing = data.isPiercing;
		_headshotChance = data.headshotChance;
		_ricochetCount = data.ricochetCount;
		_ricochetRadius = data.ricochetRadius;
		_critChance = data.critChance;
		_critDamageMin = data.critDamageMin;
		_critDamageMax = data.critDamageMax;
		_knockbackForce = data.knockbackForce;
		_playerWeight = data.playerWeight;
		_moveDirection = transform.forward;
		if (data.wallBounce)
		{
			_bounceEnabled = true;
			_velocity = transform.forward * _speed;
		}
	}

	/// <summary>
	/// Arc용 초기화.
	/// </summary>
	public void Init(float damage, Vector3 targetPos)
	{
		_damage = damage;
		_startPos = transform.position;
		_targetPos = targetPos;
		ClampArcTarget();
	}

	public void Init(float damage, Vector3 targetPos, float height, float duration)
	{
		_damage = damage;
		_startPos = transform.position;
		_targetPos = targetPos;
		_arcHeight = height;
		_arcDuration = duration;
		ClampArcTarget();
	}

	/// <summary>
	/// Arc targetPos를 맵 경계 내로 클램프한다.
	/// </summary>
	private void ClampArcTarget()
	{
		TileMap tileMap = StageManager.Instance?.TileMap;
		if (tileMap == null) return;
		tileMap.GetWorldBounds(out float minX, out float maxX, out float minZ, out float maxZ);
		_targetPos.x = Mathf.Clamp(_targetPos.x, minX, maxX);
		_targetPos.z = Mathf.Clamp(_targetPos.z, minZ, maxZ);
	}

	private void OnEnable()
	{
		transform.localScale = _originalScale;
		ResetState();

		// 몬스터 발사체 속도 감속
		if (!_isPlayerProjectile)
		{
			PlayerUpgrade upgrade = PlayerController.Instance?.GetComponent<PlayerUpgrade>();
			if (upgrade != null)
				_speed = _baseSpeed * upgrade.SlowProjectileMultiplier;
			else
				_speed = _baseSpeed;
		}

		if (_bounceEnabled)
			_velocity = transform.forward * _speed;

		if (_moveType != EProjectileMoveType.Arc)
			_lifeCoroutine = StartCoroutine(LifeTimerRoutine());

		PlayFireSound();
	}

	private void PlayFireSound()
	{
		if (Time.time - _lastFireSfxTime < SFX_COOLDOWN) return;
		_lastFireSfxTime = Time.time;
		if (_launchSound != null)
			AudioManager.Instance?.PlaySfx(_launchSound);
		else if (_isPlayerProjectile)
			AudioManager.Instance?.PlayPlayerProjectile();
		else
			AudioManager.Instance?.PlayMonsterProjectile();
	}

	private void PlayHitSound()
	{
		if (Time.time - _lastHitSfxTime < SFX_COOLDOWN) return;
		_lastHitSfxTime = Time.time;
		AudioManager.Instance?.PlayHit();
	}

	private void ResetState()
	{
		_hasHit = false;
		_destroyed = false;
		_timer = _lifeTime;
		_elapsed = 0f;
		_bounceCount = 0;
		_hitMonsterIds.Clear();
		_isPiercing = false;
		_headshotChance = 0f;
		_ricochetCount = 0;
		_ricochetRadius = 0f;
		_critChance = 0f;
		_critDamageMin = 0f;
		_critDamageMax = 0f;
		_velocity = Vector3.zero;
		_bounceEnabled = _defaultBounceEnabled;
		_speed = _baseSpeed;
		_lifeCoroutine = null;
	}

	private IEnumerator LifeTimerRoutine()
	{
		yield return new WaitForSeconds(_lifeTime);
		ReturnToPool();
	}

	private void Update()
	{
		if (_hasHit || _destroyed) return;

		switch (_moveType)
		{
			case EProjectileMoveType.Straight:
			case EProjectileMoveType.Piercing:
				UpdateStraight();
				break;
			case EProjectileMoveType.Arc:
				UpdateArc();
				break;
		}
	}

	private void UpdateStraight()
	{
		if (_bounceEnabled)
		{
			_timer -= Time.deltaTime;
			if (_timer <= 0f)
			{
				Explode();
				return;
			}

			float dt = Time.deltaTime;
			float stepDist = _velocity.magnitude * dt;

			if (stepDist > 0f && Physics.SphereCast(transform.position, _bounceRadius, _velocity.normalized, out RaycastHit hit, stepDist))
			{
				if (hit.collider.GetComponent<BlockObstacle>() != null)
				{
					Vector3 normal = hit.normal;
					normal.y = 0;
					normal.Normalize();
					_velocity = Vector3.Reflect(_velocity, normal);
					_velocity.y = 0;
					_bounceCount++;

					if (_bounceCount >= _maxBounces)
					{
						Explode();
						return;
					}
				}
			}

			transform.position += _velocity * dt;

			if (_velocity.sqrMagnitude > 0.001f)
				transform.rotation = Quaternion.LookRotation(_velocity);
		}
		else
		{
			transform.position += _moveDirection * _speed * Time.deltaTime;
		}

		if (_isPlayerProjectile && _spinSpeed > 0f)
			transform.Rotate(Vector3.up, _spinSpeed * Time.deltaTime, Space.Self);
	}

	private void UpdateArc()
	{
		_elapsed += Time.deltaTime;
		float t = _elapsed / _arcDuration;

		if (t >= 1f)
		{
			transform.position = _targetPos;
			Explode();
			return;
		}

		Vector3 pos = Vector3.Lerp(_startPos, _targetPos, t);
		pos.y += _arcHeight * 4f * t * (1f - t);
		transform.position = pos;

		TileMap tileMap = StageManager.Instance?.TileMap;
		if (tileMap != null)
		{
			tileMap.GetWorldBounds(out float minX, out float maxX, out float minZ, out float maxZ);
			if (pos.x < minX || pos.x > maxX || pos.z < minZ || pos.z > maxZ)
			{
				Explode();
				return;
			}
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (_hasHit) return;

		// Block 충돌 처리
		if (other.GetComponent<BlockObstacle>() != null)
		{
			if (_moveType == EProjectileMoveType.Arc) return;
			if (_bounceEnabled) return; // SphereCast에서 처리

			_hasHit = true;
			ReturnToPool();
			return;
		}

		if (_isPlayerProjectile)
		{
			MonsterBase monster = other.GetComponent<MonsterBase>();
			if (monster != null)
			{
				int mId = monster.GetInstanceID();
				if (_hitMonsterIds.Contains(mId))
					return;
				_hitMonsterIds.Add(mId);
				PlayHitSound();

				// 헤드샷 판정
				if (_headshotChance > 0f && !monster.IsBoss && Random.value < _headshotChance)
				{
					monster.InstantKill();
					SpawnHitEffect(monster.transform.position);
					DamageTextSpawner.SpawnHeadshot(monster.transform.position + Vector3.up);
				}
				else
				{
					float finalDamage = _damage;
					bool isCrit = _critChance > 0f && Random.value < _critChance;
					if (isCrit)
						finalDamage *= Random.Range(_critDamageMin, _critDamageMax);

					if (_knockbackForce > 0f && !monster.IsImmovable && !monster.IsBoss && monster.Weight < _playerWeight)
						monster.Knockback(transform.forward, _knockbackForce);
					monster.TakeDamage(finalDamage, isCrit);
					SpawnHitEffect(monster.transform.position);
				}

				// 반동(리코셰) > 관통 > 파괴
				if (_ricochetCount > 0)
				{
					_ricochetCount--;
					MonsterBase next = FindNearestMonster(_ricochetRadius);
					if (next != null)
					{
						Vector3 dir = (next.transform.position - transform.position).normalized;
						dir.y = 0;
						transform.forward = dir;
						_moveDirection = dir;
						if (_bounceEnabled)
							_velocity = dir * _speed;
						return;
					}
				}

				if (_isPiercing)
					return;

				_hasHit = true;
				ReturnToPool();
			}
		}
		else
		{
			PlayerController player = other.GetComponent<PlayerController>();
			if (player != null)
			{
				_hasHit = true;
				PlayHitSound();
				player.TakeDamage(_damage);
				SpawnHitEffect(player.transform.position);

				if (_bounceEnabled)
					Explode();
				else
					ReturnToPool();
			}
		}
	}

	/// <summary>
	/// 반경 내 가장 가까운 미피격 몬스터를 찾는다.
	/// </summary>
	private MonsterBase FindNearestMonster(float radius)
	{
		var monsters = StageManager.Instance.AliveMonsters;
		MonsterBase nearest = null;
		float minDist = radius;

		for (int i = 0; i < monsters.Count; i++)
		{
			MonsterBase m = monsters[i];
			if (m == null || m.CurrentHp <= 0) continue;
			if (_hitMonsterIds.Contains(m.GetInstanceID())) continue;

			float dist = Vector3.Distance(transform.position, m.transform.position);
			if (dist < minDist)
			{
				minDist = dist;
				nearest = m;
			}
		}

		return nearest;
	}

	private void SpawnHitEffect(Vector3 position)
	{
		if (_hitEffectPrefab == null) return;
		GameObject fx = ObjectPool.Instance.Get(_hitEffectPrefab);
		fx.transform.position = position;
		fx.transform.rotation = Quaternion.identity;
	}

	private void Explode()
	{
		if (_destroyed) return;
		_destroyed = true;

		if (_explosionPrefab != null)
		{
			GameObject explosion = ObjectPool.Instance.Get(_explosionPrefab);
			explosion.transform.position = transform.position;
			explosion.transform.rotation = Quaternion.identity;
			FireExplosion fe = explosion.GetComponent<FireExplosion>();
			if (fe != null)
				fe.Init(_damage);
		}

		ReturnToPool();
	}

	private void ReturnToPool()
	{
		if (_lifeCoroutine != null)
		{
			StopCoroutine(_lifeCoroutine);
			_lifeCoroutine = null;
		}
		ObjectPool.Instance.Return(gameObject);
	}
}
