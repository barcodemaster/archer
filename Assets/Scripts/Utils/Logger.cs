using UnityEngine;

/// <summary>
/// 커스텀 로거. 에디터에서만 로그를 출력하고 빌드에서는 성능 오버헤드를 제거한다.
/// </summary>
public static class Logger
{
	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public static void Log(string tag, string message)
	{
		Debug.Log($"[{tag}] {message}");
	}

	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public static void LogWarning(string tag, string message)
	{
		Debug.LogWarning($"[{tag}] {message}");
	}

	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public static void LogError(string tag, string message)
	{
		Debug.LogError($"[{tag}] {message}");
	}
}
