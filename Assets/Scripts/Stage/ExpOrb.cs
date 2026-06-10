using UnityEngine;

/// <summary>
/// 몬스터 사망 시 드롭되어 대기하다가, 스테이지 클리어 시 플레이어에게 날아가 경험치를 전달한다.
/// </summary>
public class ExpOrb : MonoBehaviour
{
	[SerializeField] private float _flySpeed = 8f;
	[SerializeField] private float _arriveDistance = 0.3f;
	[SerializeField] private float _bobAmplitude = 0.2f;
	[SerializeField] private float _bobFrequency = 2f;

	private int _expAmount;
	private Transform _target;
	private bool _waiting;
	private bool _collecting;
	private Vector3 _spawnPos;
	private float _elapsed;

	/// <summary>
	/// 경험치 양을 설정하고 대기 상태로 진입한다.
	/// </summary>
	public void Init(int expAmount)
	{
		_expAmount = expAmount;
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
		PlayerController player = PlayerController.Instance;
		if (player != null)
			_target = player.transform;
		_elapsed = 0f;
	}

	private void Update()
	{
		if (_waiting)
		{
			// bobbing 애니메이션
			float yOffset = Mathf.Sin(Time.time * _bobFrequency) * _bobAmplitude;
			transform.position = _spawnPos + Vector3.up * yOffset;
			return;
		}

		if (!_collecting) return;

		if (_target == null)
		{
			AudioManager.Instance?.PlayExpCollect();
			ExpManager.Instance.AddExp(_expAmount);
			Destroy(gameObject);
			return;
		}

		Vector3 dir = (_target.position - transform.position);
		if (dir.magnitude <= _arriveDistance)
		{
			AudioManager.Instance?.PlayExpCollect();
			ExpManager.Instance.AddExp(_expAmount);
			Destroy(gameObject);
			return;
		}

		_elapsed += Time.deltaTime;
		float speed = Mathf.Max(_flySpeed + _elapsed * 10f, _flySpeed + dir.magnitude * 2f);
		transform.position = Vector3.MoveTowards(transform.position, _target.position, speed * Time.deltaTime);
	}
}
