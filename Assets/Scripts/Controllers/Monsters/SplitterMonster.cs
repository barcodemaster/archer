using UnityEngine;
using static Define;

/// <summary>
/// 분열 몬스터: 플레이어를 추적하는 근접 몬스터. 사망 시 자식 몬스터를 스폰한다.
/// </summary>
[RequireComponent(typeof(TilePassability))]
public class SplitterMonster : MonsterBase
{
	[Header("Movement")]
	[SerializeField] private float _moveSpeed = 2.5f;

	[Header("Split")]
	[SerializeField] private GameObject _childPrefab;
	[SerializeField] private int _childCount = 2;
	[SerializeField] private float _spawnOffset = 0.5f;

	protected override void Update()
	{
		base.Update();

		if (State == EState.Die) return;
		if (Target == null) return;

		State = EState.Move;

		Vector3 dir = GetPathDirection();

		if (dir != Vector3.zero)
			transform.rotation = Quaternion.LookRotation(dir);

		MoveToward(dir, _moveSpeed);
	}

	protected override void OnDie()
	{
		if (_childPrefab == null) return;

		Vector3 right = transform.right;

		for (int i = 0; i < _childCount; i++)
		{
			float offset = (i - (_childCount - 1) / 2f) * _spawnOffset;
			Vector3 spawnPos = transform.position + right * offset;
			GameObject child = Instantiate(_childPrefab, spawnPos, transform.rotation);
			MonsterBase childMonster = child.GetComponent<MonsterBase>();
			if (childMonster != null)
				StageManager.Instance.RegisterMonster(childMonster);
		}
	}
}
