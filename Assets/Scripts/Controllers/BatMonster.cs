using UnityEngine;
using static Define;

/// <summary>
/// 박쥐 몬스터: 플레이어를 직선으로 추적하다가 주기적으로 빠르게 돌진한다.
/// Fly 플래그로 벽을 통과하며, 플레이어와 겹쳐도 트리거 데미지를 입힌다.
/// </summary>
[RequireComponent(typeof(TilePassability))]
public class BatMonster : MonsterBase
{
	[SerializeField] private float _chaseSpeed = 2f;
	[SerializeField] private float _chargeIntervalMin = 2f;
	[SerializeField] private float _chargeIntervalMax = 5f;
	[SerializeField] private float _chargeSpeed = 12f;
	[SerializeField] private float _chargeDistance = 6f;

	private float _chargeTimer;
	private Vector3 _chargeDirection;
	private float _chargedDistance;

	private void Awake()
	{
		// 겹침 허용: Rigidbody/물리콜라이더를 추가하지 않음
		_overlapWithPlayer = true;
	}

	protected override void Start()
	{
		base.Start();
		_chargeTimer = Random.Range(_chargeIntervalMin, _chargeIntervalMax);
	}

	protected override void Update()
	{
		base.Update();

		if (State == EState.Die) return;
		if (Target == null) return;

		if (State == EState.Attack)
		{
			// Charge: 기록된 방향으로 직진
			if (_chargeDirection != Vector3.zero)
				transform.rotation = Quaternion.LookRotation(_chargeDirection);

			float step = _chargeSpeed * Time.deltaTime;
			Vector3 chargeNext = transform.position + _chargeDirection * step;
			if (!CanMoveTo(chargeNext))
			{
				// 장애물에 막힘 → 돌진 중단
				_chargeTimer = Random.Range(_chargeIntervalMin, _chargeIntervalMax);
				_chargedDistance = 0f;
				State = EState.Move;
				return;
			}
			transform.position = chargeNext;
			_chargedDistance += step;

			if (_chargedDistance >= _chargeDistance)
			{
				// 돌진 완료 → Chase 복귀
				_chargeTimer = Random.Range(_chargeIntervalMin, _chargeIntervalMax);
				_chargedDistance = 0f;
				State = EState.Move;
			}
		}
		else
		{
			// Chase: A* 경로로 플레이어 추적 (Block 우회)
			State = EState.Move;

			RefreshPath();
			Vector3 dir = GetPathDirection();

			if (dir != Vector3.zero)
				transform.rotation = Quaternion.LookRotation(dir);

			MoveToward(dir, _chaseSpeed);

			_chargeTimer -= Time.deltaTime;
			if (_chargeTimer <= 0f)
			{
				// 돌진 시작: 현재 플레이어 방향으로 직진
				Vector3 toPlayer = (Target.position - transform.position);
				toPlayer.y = 0;
				_chargeDirection = toPlayer.normalized;
				_chargedDistance = 0f;
				State = EState.Attack;
			}
		}
	}
}
