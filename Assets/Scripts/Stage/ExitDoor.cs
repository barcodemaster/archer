using System.Collections;
using UnityEngine;

public class ExitDoor : MonoBehaviour
{
	[SerializeField] private GameObject _openDoorPrefab;
	[SerializeField] private float _openDelay = 1.5f;
	[SerializeField] private float _lightIntensity = 50f;
	[SerializeField] private Color _lightColor = Color.yellow;

	private GameObject _blocker;
	private Collider _collider;

	private void Start()
	{
		_collider = GetComponent<Collider>();
		if (_collider != null)
			_collider.enabled = false;

		Transform blockerTransform = transform.Find("Blocker");
		if (blockerTransform != null)
			_blocker = blockerTransform.gameObject;
	}

	/// <summary>
	/// 클리어 시 딜레이 후 문을 열고 빛/파티클 이펙트를 생성한다.
	/// </summary>
	public void OpenDoor()
	{
		StartCoroutine(OpenDoorSequence());
	}

	private IEnumerator OpenDoorSequence()
	{
		yield return new WaitForSeconds(_openDelay);

		// 계단 차단 해제
		if (_blocker != null)
			_blocker.SetActive(false);

		// ExitDoor의 모든 렌더러 비활성화 (비주얼 숨김, 콜라이더는 유지)
		Renderer[] renderers = GetComponentsInChildren<Renderer>();
		foreach (var r in renderers)
			r.enabled = false;

		// 열린 문 프리팹 생성
		if (_openDoorPrefab != null)
		{
			GameObject openDoor = Instantiate(_openDoorPrefab, transform.position, transform.rotation, transform);
			// OpenDoor 콜라이더 제거 (ExitDoor 트리거 간섭 방지)
			Collider[] cols = openDoor.GetComponentsInChildren<Collider>();
			foreach (var c in cols)
				Destroy(c);
		}

		// Point Light 생성
		GameObject lightObj = new GameObject("DoorLight");
		lightObj.transform.SetParent(transform);
		lightObj.transform.localPosition = Vector3.up * 1.5f;
		Light pointLight = lightObj.AddComponent<Light>();
		pointLight.type = LightType.Point;
		pointLight.color = _lightColor;
		pointLight.intensity = _lightIntensity;
		pointLight.range = 8f;

		// Particle 이펙트 생성
		GameObject particleObj = new GameObject("DoorParticle");
		particleObj.transform.SetParent(transform);
		particleObj.transform.localPosition = Vector3.up * 0.5f;
		ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();

		var main = ps.main;
		main.startColor = new Color(1f, 0.9f, 0.3f, 1f);
		main.startSize = 0.15f;
		main.startLifetime = 1.5f;
		main.startSpeed = 1f;
		main.loop = true;
		main.simulationSpace = ParticleSystemSimulationSpace.World;

		var emission = ps.emission;
		emission.rateOverTime = 20f;

		var shape = ps.shape;
		shape.shapeType = ParticleSystemShapeType.Cone;
		shape.angle = 25f;
		shape.radius = 0.3f;

		var colorOverLifetime = ps.colorOverLifetime;
		colorOverLifetime.enabled = true;
		Gradient grad = new Gradient();
		grad.SetKeys(
			new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.9f, 0.3f), 0f), new GradientColorKey(new Color(1f, 0.6f, 0f), 1f) },
			new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
		);
		colorOverLifetime.color = grad;

		// URP 호환 머티리얼 할당 (Built-in 기본 머티리얼은 URP에서 분홍색으로 표시됨)
		var psRenderer = particleObj.GetComponent<ParticleSystemRenderer>();
		Material particleMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
		particleMat.SetColor("_BaseColor", new Color(1f, 0.9f, 0.3f, 1f));
		psRenderer.material = particleMat;

		if (_collider != null)
			_collider.enabled = true;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.GetComponent<PlayerController>() != null)
		{
			StageManager.Instance.GoToNextStage();
		}
	}
}
