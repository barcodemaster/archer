using System.Collections.Generic;
using UnityEngine;
using static Define;

/// <summary>
/// 펫 FSM 호스트. 상태(Patrol/Return/Chase/Attack)에 따라 자율적으로 행동한다.
/// </summary>
public class PetController : MonoBehaviour
{
	[SerializeField] private bool _useArc;
	[SerializeField] private GameObject _projectilePrefab;

	private EquipmentData _data;
	private EquipmentTable _table;
	private EEquipSlot _slot;
	private float _attackDamage;
	private Animator _animator;
	private IPetState _currentState;

	// --- A* 경로 탐색 ---
	private List<Vector2Int> _path = new();
	private int _pathIndex;
	private float _pathRefreshTimer;

	// --- 상태 공유 데이터 ---
	public Vector3 LockedReturnTarget { get; set; }
	public MonsterBase ChaseTarget { get; set; }
	public Vector3 PatrolTarget { get; set; }
	public bool HasPatrolTarget { get; set; }
	public float StateTimer { get; set; }
	public float AttackTimer { get; set; }
	public float AttackCooldown { get; private set; }
	public float PatrolWaitTimer { get; set; }

	public int TableId => _data?.tableId ?? -1;

	/// <summary>
	/// 플레이어 기준 offset 월드 좌표를 반환한다.
	/// </summary>
	public Vector3 OffsetPosition
	{
		get
		{
			if (PlayerController.Instance == null) return transform.position;
			Transform player = PlayerController.Instance.transform;
			Vector3 offset = _slot == EEquipSlot.Pet1
				? GameConfig.Instance.petOffset1
				: GameConfig.Instance.petOffset2;
			Vector3 rotated = player.rotation * offset;
			rotated.y = 0f;
			return player.position + rotated;
		}
	}

	/// <summary>
	/// 플레이어와의 거리를 반환한다.
	/// </summary>
	public float DistanceToPlayer
	{
		get
		{
			if (PlayerController.Instance == null) return 0f;
			Vector3 a = transform.position;
			Vector3 b = PlayerController.Instance.transform.position;
			float dx = a.x - b.x;
			float dz = a.z - b.z;
			return Mathf.Sqrt(dx * dx + dz * dz);
		}
	}

	/// <summary>
	/// PetSpawner에서 호출. 초기 데이터를 설정하고 FSM을 시작한다.
	/// </summary>
	public void Init(EquipmentData data, EquipmentTable table, EEquipSlot slot)
	{
		_data = data;
		_table = table;
		_slot = slot;
		_animator = GetComponent<Animator>();
		if (_animator != null)
			_animator.applyRootMotion = false;

		transform.localScale = Vector3.one * GameConfig.Instance.petScale;
		transform.position = OffsetPosition;

		RefreshStats();
		TransitionTo(PetPatrolState.Instance);
	}

	/// <summary>
	/// 장비 레벨업 등으로 스탯이 변경될 때 호출된다.
	/// </summary>
	public void RefreshStats()
	{
		if (_data == null || _table == null) return;
		_attackDamage = _data.GetMainStat(_table);

		float cd = _table.attackCooldown;
		AttackCooldown = cd > 0f ? cd : GameConfig.Instance.petAttackCooldown;
	}

	private void Update()
	{
		if (PlayerController.Instance == null) return;
		_currentState?.Update(this);
	}

	/// <summary>
	/// 상태를 전환한다. Exit → Enter 순서로 호출한다.
	/// 경로를 초기화하여 새 상태 진입 즉시 경로를 재계산한다.
	/// </summary>
	public void TransitionTo(IPetState newState)
	{
		_currentState?.Exit(this);
		_currentState = newState;
		_path.Clear();
		_pathRefreshTimer = 0f;
		_currentState?.Enter(this);
	}

	/// <summary>
	/// 해당 월드 좌표로 이동 가능한지 TileMap을 통해 확인한다.
	/// </summary>
	public bool CanMoveTo(Vector3 worldPos)
	{
		TileMap tileMap = StageManager.Instance?.TileMap;
		if (tileMap == null) return true;
		return tileMap.CanPass(worldPos, ETilePassFlag.Walk);
	}

	/// <summary>
	/// 목표 월드 좌표까지 A* 경로를 탐색하고 경로를 갱신한다.
	/// </summary>
	private void RefreshPath(Vector3 targetWorld)
	{
		TileMap tileMap = StageManager.Instance?.TileMap;
		if (tileMap == null) { _path.Clear(); return; }
		Vector2Int myGrid = tileMap.WorldToGrid(transform.position);
		Vector2Int targetGrid = tileMap.WorldToGrid(targetWorld);
		_path = AStarPathfinder.FindPath(tileMap, myGrid, targetGrid, ETilePassFlag.Walk);
		_pathIndex = 0;
	}

	/// <summary>
	/// 현재 A* 경로를 따라가는 방향을 반환한다. 경로가 없으면 목표 직선 방향을 반환한다.
	/// </summary>
	private Vector3 GetPathDirection(Vector3 fallbackTarget)
	{
		if (_path == null || _pathIndex >= _path.Count)
		{
			Vector3 direct = fallbackTarget - transform.position;
			direct.y = 0f;
			return direct.sqrMagnitude > 0.001f ? direct.normalized : Vector3.zero;
		}

		TileMap tileMap = StageManager.Instance?.TileMap;
		if (tileMap == null)
		{
			_path.Clear();
			return Vector3.zero;
		}

		Vector3 waypoint = tileMap.GridToWorld(_path[_pathIndex].x, _path[_pathIndex].y);
		waypoint.y = transform.position.y;

		// 현재 웨이포인트에 충분히 가까워졌으면 다음으로 진행
		if ((waypoint - transform.position).sqrMagnitude < 0.3f * 0.3f)
		{
			_pathIndex++;
			if (_pathIndex >= _path.Count)
			{
				Vector3 direct = fallbackTarget - transform.position;
				direct.y = 0f;
				return direct.sqrMagnitude > 0.001f ? direct.normalized : Vector3.zero;
			}
			waypoint = tileMap.GridToWorld(_path[_pathIndex].x, _path[_pathIndex].y);
			waypoint.y = transform.position.y;
		}

		Vector3 dir = waypoint - transform.position;
		dir.y = 0f;
		return dir.sqrMagnitude > 0.001f ? dir.normalized : Vector3.zero;
	}

	/// <summary>
	/// 목표 지점으로 A* 경로 추종 이동 + 방향 회전.
	/// 벽/블록에 막히면 슬라이딩을 최후 보호막으로 적용하고, 맵 경계로 클램핑한다.
	/// </summary>
	public void MoveTo(Vector3 target, float speed)
	{
		// 경로 갱신 타이머
		_pathRefreshTimer -= Time.deltaTime;
		if (_pathRefreshTimer <= 0f)
		{
			RefreshPath(target);
			_pathRefreshTimer = GameConfig.Instance.pathRefreshInterval;
		}

		Vector3 dir = GetPathDirection(target);
		if (dir.sqrMagnitude < 0.001f)
		{
			FaceDirection(target);
			return;
		}

		Vector3 pos = transform.position;
		Vector3 newPos = pos + dir * speed * Time.deltaTime;
		newPos.y = pos.y;

		// 슬라이딩 충돌 체크 (A* 경로가 완벽하지 않을 때 대비)
		if (!CanMoveTo(newPos))
		{
			Vector3 slideX = new Vector3(newPos.x, pos.y, pos.z);
			if (CanMoveTo(slideX))
			{
				newPos = slideX;
			}
			else
			{
				Vector3 slideZ = new Vector3(pos.x, pos.y, newPos.z);
				newPos = CanMoveTo(slideZ) ? slideZ : pos;
			}
		}

		// 맵 경계 클램핑 (이중 보호)
		TileMap tileMap = StageManager.Instance?.TileMap;
		if (tileMap != null)
		{
			tileMap.GetWorldBounds(out float minX, out float maxX, out float minZ, out float maxZ);
			newPos.x = Mathf.Clamp(newPos.x, minX + 0.5f, maxX - 0.5f);
			newPos.z = Mathf.Clamp(newPos.z, minZ + 0.5f, maxZ - 0.5f);
		}

		transform.position = newPos;
		FaceDirection(target);
	}

	/// <summary>
	/// 지정 월드 좌표를 바라보도록 회전한다.
	/// </summary>
	public void FaceDirection(Vector3 worldTarget)
	{
		Vector3 dir = worldTarget - transform.position;
		dir.y = 0f;
		if (dir.sqrMagnitude < 0.001f) return;
		transform.rotation = Quaternion.LookRotation(dir);
	}

	/// <summary>
	/// 발사체를 발사한다. _useArc에 따라 Arc/Straight 분기.
	/// </summary>
	public void FireProjectile()
	{
		if (ChaseTarget == null || _projectilePrefab == null) return;

		ProjectileBase proj;
		if (_useArc)
		{
			proj = ProjectileFactory.CreateArc(_projectilePrefab, transform.position,
				ChaseTarget.transform.position, _attackDamage);
		}
		else
		{
			Vector3 dir = (ChaseTarget.transform.position - transform.position);
			dir.y = 0f;
			dir.Normalize();
			proj = ProjectileFactory.Create(_projectilePrefab, transform.position, dir, _attackDamage);
		}

		if (proj != null)
			proj.SetPlayerProjectile(true);
	}

	/// <summary>
	/// 범위 내 가장 가까운 살아있는 몬스터를 반환한다.
	/// </summary>
	public MonsterBase FindNearestMonster(float range)
	{
		IReadOnlyList<MonsterBase> monsters = StageManager.Instance?.AliveMonsters;
		if (monsters == null || monsters.Count == 0) return null;

		float rangeSqr = range * range;
		float minDist = float.MaxValue;
		MonsterBase nearest = null;
		Vector3 myPos = transform.position;

		for (int i = 0; i < monsters.Count; i++)
		{
			if (monsters[i] == null || monsters[i].IsDead) continue;
			float dist = (monsters[i].transform.position - myPos).sqrMagnitude;
			if (dist < rangeSqr && dist < minDist)
			{
				minDist = dist;
				nearest = monsters[i];
			}
		}

		return nearest;
	}

	/// <summary>
	/// 플레이어 offset 위치로 즉시 이동하고 Patrol 상태로 전환한다.
	/// </summary>
	public void TeleportToOffset()
	{
		transform.position = OffsetPosition;
		TransitionTo(PetPatrolState.Instance);
	}

	/// <summary>
	/// 애니메이션을 CrossFade로 재생한다.
	/// </summary>
	public void PlayAnimation(int animHash)
	{
		if (_animator == null) return;
		_animator.CrossFade(animHash, 0.1f);
	}
}
