using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Queue 기반 오브젝트 풀. 프리팹 이름을 키로 사용하며,
/// 1분마다 미사용 오브젝트를 정리한다.
/// </summary>
public class ObjectPool : Singleton<ObjectPool>
{
	private struct PoolItem
	{
		public GameObject obj;
		public float returnTime;
	}

	private readonly Dictionary<string, Queue<PoolItem>> _pools = new();
	private readonly Dictionary<GameObject, string> _keyMap = new();
	private const float CLEANUP_INTERVAL = 60f;
	private const float EXPIRE_TIME = 60f;

	private void Start()
	{
		StartCoroutine(CleanupRoutine());
	}

	/// <summary>
	/// 풀에서 오브젝트를 꺼낸다. 없으면 새로 생성한다.
	/// </summary>
	public GameObject Get(GameObject prefab)
	{
		string key = prefab.name;

		if (_pools.TryGetValue(key, out Queue<PoolItem> queue))
		{
			while (queue.Count > 0)
			{
				PoolItem item = queue.Dequeue();
				if (item.obj != null)
				{
					item.obj.SetActive(true);
					return item.obj;
				}
			}
		}

		GameObject obj = Instantiate(prefab);
		obj.name = prefab.name;
		_keyMap[obj] = key;
		return obj;
	}

	/// <summary>
	/// 오브젝트를 비활성화하고 풀에 반환한다.
	/// </summary>
	public void Return(GameObject obj)
	{
		if (obj == null) return;

		obj.SetActive(false);

		if (!_keyMap.TryGetValue(obj, out string key))
		{
			Destroy(obj);
			return;
		}

		if (!_pools.ContainsKey(key))
			_pools[key] = new Queue<PoolItem>();

		_pools[key].Enqueue(new PoolItem { obj = obj, returnTime = Time.time });
	}

	/// <summary>
	/// 1분마다 반환 후 1분 이상 경과한 오브젝트를 삭제한다.
	/// </summary>
	private IEnumerator CleanupRoutine()
	{
		WaitForSeconds wait = new WaitForSeconds(CLEANUP_INTERVAL);

		while (true)
		{
			yield return wait;

			foreach (var kvp in _pools)
			{
				Queue<PoolItem> queue = kvp.Value;
				int count = queue.Count;

				for (int i = 0; i < count; i++)
				{
					PoolItem item = queue.Dequeue();

					if (item.obj == null)
						continue;

					if (Time.time - item.returnTime >= EXPIRE_TIME)
					{
						_keyMap.Remove(item.obj);
						Destroy(item.obj);
					}
					else
					{
						queue.Enqueue(item);
					}
				}
			}
		}
	}
}
