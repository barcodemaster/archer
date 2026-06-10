using UnityEngine;
using UnityEngine.SceneManagement;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
	private static T _instance;
	private static bool _destroyed;

	public static T Instance
	{
		get
		{
			if (_destroyed)
				return null;

			if (_instance == null)
			{
				_instance = (T)FindAnyObjectByType(typeof(T));

				if (_instance == null)
				{
					GameObject go = new GameObject($"@{typeof(T).Name}");
					_instance = go.AddComponent<T>();
				}

				DontDestroyOnLoad(_instance.gameObject);
			}

			return _instance;
		}
	}

	protected virtual void Awake()
	{
		if (_instance != null && _instance != this)
		{
			Destroy(gameObject);
			return;
		}

		_instance = this as T;
		_destroyed = false;
		DontDestroyOnLoad(gameObject);
	}

	protected virtual void OnDestroy()
	{
		if (_instance == this)
		{
			_instance = null;
			_destroyed = true;
			SceneManager.sceneLoaded += OnSceneLoaded;
		}
	}

	protected virtual void OnApplicationQuit()
	{
		_destroyed = true;
	}

	private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		_destroyed = false;
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}
}
