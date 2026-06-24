using UnityEngine;

/// <summary>
/// 맵 가장자리(하단/좌/우)에 반투명 안개 Quad를 배치하여
/// 맵 바깥 경계를 자연스럽게 감춘다.
/// </summary>
public class EdgeFogController : MonoBehaviour
{
    [Header("Fog Settings")]
    [SerializeField] private Color _fogColor = new Color(1f, 1f, 1f, 0.6f);
    [SerializeField] private float _fogDepth = 5f;
    [SerializeField] private float _fogY = 0.1f;
    [SerializeField] private float _extraWidth = 10f;

    private Transform _fogContainer;

    /// <summary>
    /// TileMap 경계에 맞춰 3방향 안개 Quad를 생성한다.
    /// </summary>
    public void GenerateFog(TileMap tileMap)
    {
        ClearFog();
        if (tileMap == null) return;

        tileMap.GetWorldBounds(out float minX, out float maxX, out float minZ, out float maxZ);

        _fogContainer = new GameObject("_EdgeFog").transform;
        _fogContainer.SetParent(transform);
        _fogContainer.localPosition = Vector3.zero;

        float mapWidth = maxX - minX;
        float mapHeight = maxZ - minZ;
        float totalWidth = mapWidth + _extraWidth * 2f;

        float centerX = (minX + maxX) / 2f;

        // 하단 안개
        CreateFogQuad("FogBottom",
            new Vector3(centerX, _fogY, minZ - _fogDepth / 2f),
            new Vector3(totalWidth, _fogDepth, 1f));

        // 좌측 안개
        float leftCenterZ = (minZ + maxZ) / 2f;
        CreateFogQuad("FogLeft",
            new Vector3(minX - _fogDepth / 2f, _fogY, leftCenterZ),
            new Vector3(_fogDepth, mapHeight + _fogDepth * 2f, 1f));

        // 우측 안개
        CreateFogQuad("FogRight",
            new Vector3(maxX + _fogDepth / 2f, _fogY, leftCenterZ),
            new Vector3(_fogDepth, mapHeight + _fogDepth * 2f, 1f));
    }

    private void CreateFogQuad(string name, Vector3 position, Vector3 size)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;
        quad.transform.SetParent(_fogContainer);
        quad.transform.position = position;
        quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        quad.transform.localScale = new Vector3(size.x, size.y, 1f);

        // 콜라이더 제거
        Object.DestroyImmediate(quad.GetComponent<MeshCollider>());

        Renderer rend = quad.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = _fogColor;
        rend.material = mat;
    }

    /// <summary>
    /// 생성된 안개 오브젝트를 모두 제거한다.
    /// </summary>
    public void ClearFog()
    {
        if (_fogContainer != null)
        {
            if (Application.isPlaying)
                Destroy(_fogContainer.gameObject);
            else
                DestroyImmediate(_fogContainer.gameObject);
            _fogContainer = null;
        }
    }
}
