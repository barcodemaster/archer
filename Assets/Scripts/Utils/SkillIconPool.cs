using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 풀링을 이용해 아이콘 프리팹을 재활용합니다.
public class SkillIconPool : MonoBehaviour
{
    [SerializeField] private GameObject _iconPrefab; // SkillIconImage.prefab
    [SerializeField] private int _initialSize = 20; // 필요에 따라 조정

    private readonly Stack<GameObject> _pool = new();

    private void Awake()
    {
        // 초기 풀 생성 (prefab이 지정돼 있을 때만)
        if (_iconPrefab == null)
        {
            Debug.LogWarning("SkillIconPool: _iconPrefab is not assigned.");
            return;
        }
        for (int i = 0; i < _initialSize; i++)
        {
            var obj = Instantiate(_iconPrefab, transform);
            obj.SetActive(false);
            _pool.Push(obj);
        }
    }

    public void SetPrefab(GameObject prefab)
    {
        if (prefab != null)
            _iconPrefab = prefab;
    }

    // 풀에서 아이콘을 가져옵니다. parent 를 지정하면 해당 트랜스폼 아래에 배치됩니다.
    public GameObject Get(Transform parent = null)
    {
        GameObject obj;
        if (_pool.Count > 0)
        {
            obj = _pool.Pop();
            obj.SetActive(true);
        }
        else
        {
            obj = Instantiate(_iconPrefab);
        }
        if (parent != null)
            obj.transform.SetParent(parent, false);
        return obj;
    }

    // 사용이 끝난 아이콘을 풀에 반환합니다.
    public void Release(GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.SetParent(transform);
        _pool.Push(obj);
    }
}
