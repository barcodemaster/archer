using UnityEngine;

/// <summary>
/// 스카이박스 영역(맵 바깥)에서만 보이는 구름 컨트롤러.
/// 구름을 Y=-2에 배치하여 맵 타일(Y=0)의 depth buffer에 의해 맵 영역에서 자동 가림.
/// 카메라 기준 wrap으로 플레이어 위치와 무관하게 항상 시야 내 존재.
/// </summary>
public class CloudController : MonoBehaviour
{
    [Header("Cloud Settings")]
    [SerializeField] private Sprite[] cloudSprites;
    [SerializeField] private float driftSpeed = 2.0f;
    [SerializeField] private float wrapDistance = 40f;
    [SerializeField] private int cloudCount = 10;
    [SerializeField] private float cloudY = -2f;
    [SerializeField] private Vector2 scaleRange = new Vector2(8f, 16f);
    [SerializeField] private Vector2 alphaRange = new Vector2(0.2f, 0.4f);

    [Header("Drift Direction (normalized)")]
    [SerializeField] private Vector3 driftDirection = new Vector3(1f, 0f, 0.3f);

    private Transform[] clouds;
    private float[] cloudSpeeds;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        driftDirection = driftDirection.normalized;
        SpawnClouds();
    }

    /** 구름 Quad 오브젝트들을 생성 — 3D depth 기반 렌더링 */
    private void SpawnClouds()
    {
        clouds = new Transform[cloudCount];
        cloudSpeeds = new float[cloudCount];

        Vector3 camPos = mainCamera != null ? mainCamera.transform.position : Vector3.zero;

        for (int i = 0; i < cloudCount; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = $"Cloud_{i}";
            go.transform.SetParent(transform);

            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // XZ 평면에 눕힘
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            // 머터리얼 설정 — Unlit + 알파 블렌딩
            var mr = go.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.renderQueue = 3000; // Transparent queue

            if (cloudSprites != null && cloudSprites.Length > 0)
            {
                var sprite = cloudSprites[i % cloudSprites.Length];
                mat.mainTexture = sprite.texture;
            }

            float alpha = Random.Range(alphaRange.x, alphaRange.y);
            mat.color = new Color(1f, 1f, 1f, alpha);
            mr.material = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            float scale = Random.Range(scaleRange.x, scaleRange.y);
            go.transform.localScale = new Vector3(scale, scale, 1f);

            // 카메라 XZ 기준으로 랜덤 배치
            float x = camPos.x + Random.Range(-wrapDistance, wrapDistance);
            float z = camPos.z + Random.Range(-wrapDistance, wrapDistance);
            go.transform.position = new Vector3(x, cloudY, z);

            cloudSpeeds[i] = driftSpeed * Random.Range(0.5f, 1.5f);
            clouds[i] = go.transform;
        }
    }

    private void LateUpdate()
    {
        DriftAndWrap();
    }

    /** 구름 drift + 카메라 기준 wrap-around */
    private void DriftAndWrap()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        Vector3 camPos = mainCamera.transform.position;

        for (int i = 0; i < clouds.Length; i++)
        {
            if (clouds[i] == null) continue;

            // 순수 drift만 적용
            clouds[i].position += driftDirection * (cloudSpeeds[i] * Time.deltaTime);

            // 카메라 XZ 기준으로 wrap
            Vector3 pos = clouds[i].position;
            float dx = pos.x - camPos.x;
            float dz = pos.z - camPos.z;

            if (dx > wrapDistance) pos.x -= wrapDistance * 2f;
            else if (dx < -wrapDistance) pos.x += wrapDistance * 2f;
            if (dz > wrapDistance) pos.z -= wrapDistance * 2f;
            else if (dz < -wrapDistance) pos.z += wrapDistance * 2f;

            clouds[i].position = pos;
        }
    }
}
