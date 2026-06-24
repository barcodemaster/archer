using UnityEngine;

/// <summary>
/// TileMap 양옆에 장식 프리팹(나무 등)을 자동 배치한다.
/// TileMap 프리팹에 부착하여 스테이지별 다른 장식을 설정할 수 있다.
/// </summary>
public class MapDecorationController : MonoBehaviour
{
    [Header("Side Decorations")]
    [SerializeField] private GameObject[] _leftDecorationPrefabs;
    [SerializeField] private GameObject[] _rightDecorationPrefabs;
    [SerializeField] private float _xOffset = 1.5f;
    [SerializeField] private float _spacing = 2f;
    [SerializeField] private float _yOffset = 0f;
    [SerializeField] private int _extraExtent = 3;
    [SerializeField] private Vector2 _scaleRange = new Vector2(0.8f, 1.2f);

    private Transform _decorationContainer;

    /// <summary>
    /// TileMap 경계를 기준으로 양옆에 장식물을 배치한다.
    /// </summary>
    public void GenerateDecorations(TileMap tileMap)
    {
        ClearDecorations();
        if (tileMap == null) return;

        bool hasLeft = _leftDecorationPrefabs != null && _leftDecorationPrefabs.Length > 0;
        bool hasRight = _rightDecorationPrefabs != null && _rightDecorationPrefabs.Length > 0;
        if (!hasLeft && !hasRight) return;

        tileMap.GetWorldBounds(out float minX, out float maxX, out float minZ, out float maxZ);

        _decorationContainer = new GameObject("_Decorations").transform;
        _decorationContainer.SetParent(transform);
        _decorationContainer.localPosition = Vector3.zero;

        float startZ = minZ - _extraExtent * _spacing;
        float endZ = maxZ + _extraExtent * _spacing;

        for (float z = startZ; z <= endZ; z += _spacing)
        {
            if (hasLeft)
                SpawnDecoration(_leftDecorationPrefabs, minX - _xOffset, z);
            if (hasRight)
                SpawnDecoration(_rightDecorationPrefabs, maxX + _xOffset, z);
        }
    }

    private void SpawnDecoration(GameObject[] prefabs, float x, float z)
    {
        GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
        if (prefab == null) return;

        Vector3 pos = new Vector3(x, _yOffset, z);
        GameObject obj = Instantiate(prefab, pos, Quaternion.identity, _decorationContainer);

        float scale = Random.Range(_scaleRange.x, _scaleRange.y);
        obj.transform.localScale = Vector3.one * scale;
        obj.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
    }

    /// <summary>
    /// 기존 장식 오브젝트를 모두 제거한다.
    /// </summary>
    public void ClearDecorations()
    {
        if (_decorationContainer != null)
        {
            if (Application.isPlaying)
                Destroy(_decorationContainer.gameObject);
            else
                DestroyImmediate(_decorationContainer.gameObject);
            _decorationContainer = null;
        }
    }
}
