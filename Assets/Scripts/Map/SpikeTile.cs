using UnityEngine;
using static Define;

/// <summary>
/// 가시발판 컴포넌트.
/// 이동 중 진입 시 기본데미지(인터벌 기반, 타이머는 exit 후에도 지속),
/// 멈춰있을 때 지속데미지(exit 시 타이머 리셋).
/// </summary>
public class SpikeTile : MonoBehaviour
{
	[SerializeField] private float _baseDamage = 20f;
	[SerializeField] private float _baseDamageInterval = 1.5f;
	[SerializeField] private float _continuousDamage = 5f;
	[SerializeField] private float _continuousDamageInterval = 0.5f;

	private float _baseDamageTimer;       // 항상 흐름 (exit 후에도)
	private float _continuousDamageTimer;


	private void Update()
	{
		if (_baseDamageTimer > 0f)
			_baseDamageTimer -= Time.deltaTime;
	}

	public void OnTriggerEnter(Collider other)
	{
		PlayerController player = other.GetComponent<PlayerController>();
		if (player == null) return;
				
		_continuousDamageTimer = 0f;
		if (player.State == EState.Move && _baseDamageTimer <= 0f)
		{
			player.TakeDamage(_baseDamage);
			_baseDamageTimer = _baseDamageInterval;
		}
	}

	public void OnTriggerExit(Collider other)
	{
		if(other.GetComponent<PlayerController>() == null) return;
		_continuousDamageTimer = 0f;
	}

	public void OnTriggerStay(Collider other)
	{
		PlayerController player = other.GetComponent<PlayerController>();
		if(player == null) return;

		if (player.State == EState.Move)
		{
			if (_baseDamageTimer <= 0f)
			{
				player.TakeDamage(_baseDamage);
				_baseDamageTimer = _baseDamageInterval;
				_continuousDamageTimer = 0f;
			}
		}
		else
		{
			_continuousDamageTimer -= Time.deltaTime;
			if (_continuousDamageTimer <= 0f)
			{
				player.TakeDamage(_continuousDamage);
				_continuousDamageTimer = _continuousDamageInterval;
			}
		}
	}
}
