using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using TMPro;
using static Define;

/// <summary>
/// 장비 시스템 메인 패널 UI.
/// </summary>
public class UI_EquipmentPanel : MonoBehaviour
{
	[Header("Equip Slots")]
	[SerializeField] private UI_EquipSlot[] _equipSlots;

	[Header("Inventory")]
	[SerializeField] private Transform _inventoryContent;
	[SerializeField] private GameObject _itemSlotPrefab;

	[Header("Stats")]
	[SerializeField] private TextMeshProUGUI _attackText;
	[SerializeField] private TextMeshProUGUI _hpText;

	[Header("Detail Popup")]
	[SerializeField] private UI_ItemDetailPopup _detailPopup;

	[Header("Player Model")]
	[SerializeField] private RawImage _playerModelImage;
	[SerializeField] private Camera _modelCamera;
	[SerializeField] private Transform _modelSpawnPoint;
	[SerializeField] private GameObject _playerModelPrefab;

	[Header("Close")]
	[SerializeField] private Button _closeButton;

	private RenderTexture _renderTexture;
	private GameObject _modelInstance;
	private int _originalLightCullingMask;
	private Quaternion _originalLightRotation;
	private List<GameObject> _spawnedSlots = new List<GameObject>();

	private void Awake()
	{
		if (_closeButton != null)
			_closeButton.onClick.AddListener(OnCloseClicked);

		if (_detailPopup != null)
			_detailPopup.gameObject.SetActive(false);

		SetupScrollView();
	}

	/// <summary>
	/// 장비창을 연다.
	/// </summary>
	public void Open()
	{
		SetupPlayerModel();
		Refresh();
		EquipmentManager.Instance.OnEquipChanged += Refresh;
	}

	/// <summary>
	/// 장비창을 닫는다.
	/// </summary>
	public void Close()
	{
		EquipmentManager.Instance.OnEquipChanged -= Refresh;
		CleanupPlayerModel();
	}

	/// <summary>
	/// 전체 UI를 갱신한다.
	/// </summary>
	public void Refresh()
	{
		RefreshEquipSlots();
		RefreshInventoryList();
		RefreshStats();
		RefreshModelWeapon();
	}

	/// <summary>
	/// 클론 모델의 무기 시각을 갱신한다.
	/// </summary>
	private void RefreshModelWeapon()
	{
		if (_modelInstance == null) return;
		var equipVisual = _modelInstance.GetComponent<EquipmentVisual>();
		if (equipVisual == null) return;

		equipVisual.RefreshWeapon();

		int modelLayer = LayerMask.NameToLayer("EquipmentModel");
		if (modelLayer >= 0)
			equipVisual.SetWeaponLayer(modelLayer);
	}

	/// <summary>
	/// 장착 슬롯 UI를 갱신한다.
	/// </summary>
	private void RefreshEquipSlots()
	{
		if (_equipSlots == null) return;

		var equipped = EquipmentManager.Instance.Equipped;

		foreach (var slot in _equipSlots)
		{
			if (equipped.TryGetValue(slot.SlotType, out EquipmentData item))
			{
				EquipmentTable table = EquipmentDatabase.GetById(item.tableId);
				if (table != null)
					slot.SetItem(item, table);
				else
					slot.SetEmpty();
			}
			else
			{
				slot.SetEmpty();
			}
		}
	}

	/// <summary>
	/// 미장착 인벤토리 목록을 갱신한다.
	/// </summary>
	private void RefreshInventoryList()
	{
		// 기존 슬롯 정리 (리스트 외 잔존 자식도 모두 제거)
		foreach (var go in _spawnedSlots)
			if (go != null) Destroy(go);
		_spawnedSlots.Clear();

		if (_inventoryContent == null || _itemSlotPrefab == null) return;

		for (int i = _inventoryContent.childCount - 1; i >= 0; i--)
			Destroy(_inventoryContent.GetChild(i).gameObject);

		var inventory = EquipmentManager.Instance.Inventory;

		foreach (var item in inventory)
		{
			if (item.isEquipped) continue;

			EquipmentTable table = EquipmentDatabase.GetById(item.tableId);
			if (table == null) continue;

			GameObject go = Instantiate(_itemSlotPrefab, _inventoryContent);
			UI_ItemSlot slot = go.GetComponent<UI_ItemSlot>();
			if (slot != null)
				slot.SetItem(item, table, this);
			_spawnedSlots.Add(go);
		}
	}

	/// <summary>
	/// 총 스탯 텍스트를 갱신한다.
	/// </summary>
	private void RefreshStats()
	{
		float atk = EquipmentManager.Instance.GetTotalAttackBonus();
		float hp = EquipmentManager.Instance.GetTotalMaxHpBonus();

		if (_attackText != null)
			_attackText.text = $"ATK +{atk:F0}";
		if (_hpText != null)
			_hpText.text = $"HP +{hp:F0}";
	}

	/// <summary>
	/// 장착 슬롯 클릭 시 호출.
	/// </summary>
	public void OnEquipSlotClicked(UI_EquipSlot slot)
	{
		if (slot.Item == null) return;
		if (_detailPopup != null)
			_detailPopup.ShowEquipped(slot.Item, slot.SlotType);
	}

	/// <summary>
	/// 인벤토리 아이템 클릭 시 호출.
	/// </summary>
	public void OnItemSlotClicked(EquipmentData item)
	{
		if (_detailPopup != null)
			_detailPopup.ShowUnequipped(item);
	}

	/// <summary>
	/// RenderTexture를 사용해 플레이어 모델을 표시한다.
	/// </summary>
	private void SetupPlayerModel()
	{
		if (_playerModelImage == null)
		{
			Debug.LogWarning("[EquipmentPanel] _playerModelImage is null. Check Inspector.");
			return;
		}
		if (_modelCamera == null)
		{
			Debug.LogWarning("[EquipmentPanel] _modelCamera is null. Check Inspector.");
			return;
		}

		_renderTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
		_modelCamera.targetTexture = _renderTexture;
		_playerModelImage.texture = _renderTexture;

		// 카메라 투명 배경 설정
		_modelCamera.clearFlags = CameraClearFlags.SolidColor;
		_modelCamera.backgroundColor = Color.clear;

		// URP 카메라 설정
		var urpCamData = _modelCamera.GetComponent<UniversalAdditionalCameraData>();
		if (urpCamData == null)
			urpCamData = _modelCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
		urpCamData.renderType = CameraRenderType.Base;
		urpCamData.renderPostProcessing = false;

		// 플레이어 모델 클론 생성 (프리팹 우선, 없으면 씬에서 폴백)
		GameObject source = _playerModelPrefab;
		if (source == null)
		{
			Debug.LogWarning("[EquipmentPanel] _playerModelPrefab is null. Falling back to scene player.");
			PlayerController player = FindAnyObjectByType<PlayerController>();
			if (player != null)
				source = player.gameObject;
		}

		if (source == null)
		{
			Debug.LogWarning("[EquipmentPanel] No player source found. Model will not be displayed.");
			return;
		}

		if (_modelSpawnPoint == null)
		{
			Debug.LogWarning("[EquipmentPanel] _modelSpawnPoint is null. Check Inspector.");
			return;
		}

		_modelInstance = Instantiate(source, _modelSpawnPoint.position, _modelSpawnPoint.rotation);

		// 불필요한 컴포넌트 비활성화
		var pc = _modelInstance.GetComponent<PlayerController>();
		if (pc != null) pc.enabled = false;
		var cc = _modelInstance.GetComponent<CharacterController>();
		if (cc != null) cc.enabled = false;
		var upgrade = _modelInstance.GetComponent<PlayerUpgrade>();
		if (upgrade != null) upgrade.enabled = false;

		// EquipmentModel 레이어로 변경
		int modelLayer = LayerMask.NameToLayer("EquipmentModel");
		if (modelLayer >= 0)
			SetLayerRecursive(_modelInstance, modelLayer);
		else
			Debug.LogWarning("[EquipmentPanel] EquipmentModel layer not found. Camera culling may not work.");

		// Idle 애니메이션 재생 (자식에 Animator가 있는 경우도 대응)
		Animator anim = _modelInstance.GetComponentInChildren<Animator>();
		if (anim != null)
		{
			anim.updateMode = AnimatorUpdateMode.UnscaledTime;
			anim.Play(Define.IDLE);
		}

		// 씬 메인 라이트의 cullingMask에 모델 레이어 추가
		Light mainLight = RenderSettings.sun;
		if (mainLight != null && modelLayer >= 0)
		{
			_originalLightCullingMask = mainLight.cullingMask;
			mainLight.cullingMask |= (1 << modelLayer);
		}

		// Renderer bounds 기반 카메라 자동 프레이밍
		FrameCameraOnModel();

		// 메인 라이트를 카메라 방향으로 임시 변경 (모델 정면 조명)
		if (mainLight != null)
		{
			_originalLightRotation = mainLight.transform.rotation;
			mainLight.transform.rotation = _modelCamera.transform.rotation;
		}

		_modelCamera.enabled = true;
	}

	/// <summary>
	/// 모델의 Renderer bounds 기반으로 카메라 거리를 자동 계산하여 적절한 크기로 프레이밍한다.
	/// </summary>
	private void FrameCameraOnModel()
	{
		if (_modelInstance == null || _modelCamera == null) return;

		Renderer[] renderers = _modelInstance.GetComponentsInChildren<Renderer>();
		if (renderers.Length == 0) return;

		Bounds bounds = renderers[0].bounds;
		for (int i = 1; i < renderers.Length; i++)
			bounds.Encapsulate(renderers[i].bounds);

		// bounds 높이 기준으로 필요 거리 계산 (padding 1.5배)
		float boundsHeight = bounds.size.y;
		float boundsWidth = bounds.size.x;
		float fov = _modelCamera.fieldOfView;
		float padding = 1.5f;

		// 세로 기준 거리
		float distanceForHeight = (boundsHeight * padding * 0.5f) / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);

		// 가로 기준 거리 (aspect ratio 고려)
		float aspect = _modelCamera.aspect > 0 ? _modelCamera.aspect : 1f;
		float horizontalFov = 2f * Mathf.Atan(Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * aspect) * Mathf.Rad2Deg;
		float distanceForWidth = (boundsWidth * padding * 0.5f) / Mathf.Tan(horizontalFov * 0.5f * Mathf.Deg2Rad);

		// 더 큰 쪽 채택
		float distance = Mathf.Max(distanceForHeight, distanceForWidth);

		// 모델 정면 방향에서 같은 높이로 촬영
		Vector3 direction = _modelInstance.transform.forward;
		_modelCamera.transform.position = bounds.center + direction * distance;
		_modelCamera.transform.LookAt(bounds.center);
	}

	/// <summary>
	/// 플레이어 모델을 정리한다.
	/// </summary>
	private void CleanupPlayerModel()
	{
		if (_modelInstance != null)
			Destroy(_modelInstance);

		Light mainLight = RenderSettings.sun;
		if (mainLight != null)
		{
			mainLight.cullingMask = _originalLightCullingMask;
			mainLight.transform.rotation = _originalLightRotation;
		}

		if (_modelCamera != null)
			_modelCamera.enabled = false;

		if (_renderTexture != null)
		{
			_renderTexture.Release();
			Destroy(_renderTexture);
			_renderTexture = null;
		}
	}

	private void SetLayerRecursive(GameObject obj, int layer)
	{
		obj.layer = layer;
		foreach (Transform child in obj.transform)
			SetLayerRecursive(child.gameObject, layer);
	}

	/// <summary>
	/// ScrollView를 터치 슬라이드 전용으로 설정한다.
	/// </summary>
	private void SetupScrollView()
	{
		if (_inventoryContent == null) return;

		ScrollRect scrollRect = _inventoryContent.GetComponentInParent<ScrollRect>();
		if (scrollRect == null) return;

		scrollRect.vertical = true;
		scrollRect.horizontal = false;
		scrollRect.movementType = ScrollRect.MovementType.Elastic;

		if (scrollRect.verticalScrollbar != null)
		{
			scrollRect.verticalScrollbar.gameObject.SetActive(false);
			scrollRect.verticalScrollbar = null;
		}

		scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

		// 왼쪽 상단부터 정렬
		GridLayoutGroup grid = _inventoryContent.GetComponent<GridLayoutGroup>();
		if (grid != null)
			grid.childAlignment = TextAnchor.UpperLeft;
	}

	private void OnCloseClicked()
	{
		UIManager.Instance.HideEquipment();
	}
}
