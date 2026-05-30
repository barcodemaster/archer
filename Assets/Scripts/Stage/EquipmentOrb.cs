using UnityEngine;

/// <summary>
/// 몬스터 사망 시 드롭되어 대기하다가, 스테이지 클리어 시 플레이어에게 날아가 장비를 전달한다.
/// </summary>
public class EquipmentOrb : MonoBehaviour
{
	[SerializeField] private float _flySpeed = 8f;
	[SerializeField] private float _arriveDistance = 0.3f;
	[SerializeField] private float _bobAmplitude = 0.2f;
	[SerializeField] private float _bobFrequency = 2f;

	private EquipmentData _item;
	private Transform _target;
	private bool _waiting;
	private bool _collecting;
	private Vector3 _spawnPos;
	private float _elapsed;

	/// <summary>
	/// 장비 데이터를 설정하고 대기 상태로 진입한다.
	/// </summary>
	public void Init(EquipmentData item)
	{
		_item = item;
		_spawnPos = transform.position;
		_waiting = true;
		_collecting = false;
	}

	/// <summary>
	/// 대기 상태를 끝내고 플레이어를 향해 날아가기 시작한다.
	/// </summary>
	public void StartCollect()
	{
		_waiting = false;
		_collecting = true;
		PlayerController player = FindAnyObjectByType<PlayerController>();
		if (player != null)
			_target = player.transform;
		_elapsed = 0f;
	}

	private void Update()
	{
		if (_waiting)
		{
			float yOffset = Mathf.Sin(Time.time * _bobFrequency) * _bobAmplitude;
			transform.position = _spawnPos + Vector3.up * yOffset;
			return;
		}

		if (!_collecting) return;

		if (_target == null)
		{
			EquipmentManager.Instance.AddItem(_item);
			Destroy(gameObject);
			return;
		}

		Vector3 dir = (_target.position - transform.position);
		if (dir.magnitude <= _arriveDistance)
		{
			EquipmentManager.Instance.AddItem(_item);
			Destroy(gameObject);
			return;
		}

		_elapsed += Time.deltaTime;
		float speed = Mathf.Max(_flySpeed + _elapsed * 10f, _flySpeed + dir.magnitude * 2f);
		transform.position = Vector3.MoveTowards(transform.position, _target.position, speed * Time.deltaTime);
	}
}
