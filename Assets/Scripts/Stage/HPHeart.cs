using UnityEngine;

/// <summary>
/// 몬스터 사망 시 확률적으로 드롭되어 대기하다가, 스테이지 클리어 시 플레이어에게 날아가 HP를 회복한다.
/// </summary>
public class HPHeart : MonoBehaviour
{
	[SerializeField] private float _flySpeed = 8f;
	[SerializeField] private float _arriveDistance = 0.3f;
	[SerializeField] private float _bobAmplitude = 0.2f;
	[SerializeField] private float _bobFrequency = 2f;
	[SerializeField] private float _healAmount = 50f;

	private Transform _target;
	private bool _waiting;
	private bool _collecting;
	private Vector3 _spawnPos;
	private float _elapsed;

	/// <summary>
	/// 대기 상태로 초기화한다.
	/// </summary>
	public void Init()
	{
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
		PlayerController player = Object.FindAnyObjectByType<PlayerController>();
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
			HealPlayer();
			Destroy(gameObject);
			return;
		}

		Vector3 dir = (_target.position - transform.position);
		if (dir.magnitude <= _arriveDistance)
		{
			HealPlayer();
			Destroy(gameObject);
			return;
		}

		_elapsed += Time.deltaTime;
		float speed = Mathf.Max(_flySpeed + _elapsed * 10f, _flySpeed + dir.magnitude * 2f);
		transform.position = Vector3.MoveTowards(transform.position, _target.position, speed * Time.deltaTime);
	}

	private void HealPlayer()
	{
		PlayerController player = Object.FindAnyObjectByType<PlayerController>();
		if (player != null)
			player.Heal(_healAmount);
	}
}
