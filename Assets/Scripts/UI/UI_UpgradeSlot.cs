using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_UpgradeSlot : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI _nameText;
	[SerializeField] private Image _iconImage;
	[SerializeField] private TextMeshProUGUI _descText;
	[SerializeField] private Button _button;

	private UpgradeInfo _assigned;
	private Coroutine _spinCoroutine;

	public UpgradeInfo Assigned => _assigned;

	/// <summary>
	/// 슬롯의 표시 내용을 초기화한다.
	/// </summary>
	public void Clear()
	{
		if (_nameText != null) _nameText.text = "";
		if (_iconImage != null) _iconImage.sprite = null;
		if (_descText != null) _descText.text = "";
	}

	public void SetUpgrade(UpgradeInfo info)
	{
		_assigned = info;
	}

	public void StartSpin()
	{
		SetInteractable(false);
		_spinCoroutine = StartCoroutine(SpinCoroutine());
	}

	public void StopSpin(bool playConfirmSound = true)
	{
		if (_spinCoroutine != null)
		{
			StopCoroutine(_spinCoroutine);
			_spinCoroutine = null;
		}
		Display(_assigned);
		if (playConfirmSound)
			AudioManager.Instance?.PlayUpgradeSlotConfirm();
		StartCoroutine(PunchScale());
	}

	public void SetInteractable(bool v)
	{
		_button.interactable = v;
	}

	private IEnumerator SpinCoroutine()
	{
		UpgradeInfo[] all = UpgradeDatabase.GetAll();
		float elapsed = 0f;

		while (true)
		{
			int randomIndex = Random.Range(0, all.Length);
			Display(all[randomIndex]);
			AudioManager.Instance?.PlayUpgradeSlotSpin();
			float interval = 0.05f + elapsed * 0.02f;
			float waited = 0f;
			while (waited < interval)
			{
				waited += Time.unscaledDeltaTime;
				elapsed += Time.unscaledDeltaTime;
				yield return null;
			}
		}
	}

	private IEnumerator PunchScale()
	{
		transform.localScale = Vector3.one * 1.15f;
		float t = 0f;
		while (t < 0.2f)
		{
			t += Time.unscaledDeltaTime;
			transform.localScale = Vector3.Lerp(Vector3.one * 1.15f, Vector3.one, t / 0.2f);
			yield return null;
		}
		transform.localScale = Vector3.one;
	}

	private void Display(UpgradeInfo info)
	{
		if (_nameText != null)
			_nameText.text = info.name;
		if (_iconImage != null)
			_iconImage.sprite = info.icon;
		if (_descText != null)
			_descText.text = info.description;
	}
}
