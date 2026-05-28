using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 버튼에 붙이면 클릭 시 자동으로 사운드를 재생한다.
/// </summary>
[RequireComponent(typeof(Button))]
public class UI_ButtonSound : MonoBehaviour
{
	private void Awake()
	{
		GetComponent<Button>().onClick.AddListener(() => AudioManager.Instance?.PlayButtonClick());
	}
}
