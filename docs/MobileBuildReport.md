# 모바일 빌드 적용 보고서

> 작성일: 2026-06-06

---

## 1. 코드 변경 내역

### 1.1 Bundle ID 및 프로젝트 설정

**파일:** `ProjectSettings/ProjectSettings.asset`

| 항목 | 변경 전 | 변경 후 | 이유 |
|------|---------|---------|------|
| Bundle Identifier | `com.UnityTechnologies...` | `com.yehocompany.archer` | 템플릿 기본값으로는 Google Play / App Store 출시 불가 |
| Min Android SDK | API 25 (7.1) | API 28 (9.0) | Google Play 최소 요구사항 충족 |
| 화면 방향 | AutoRotation | Portrait 고정 (landscape 비활성화) | 세로 전용 게임에 맞게 화면 방향 고정 |

---

### 1.2 Safe Area 대응

**파일:** `Assets/Scripts/UI/SafeArea.cs` (신규 생성)

노치, 펀치홀, 다이나믹 아일랜드 등 비정형 디스플레이에서 UI 잘림을 방지한다.
`Screen.safeArea` 값을 기반으로 `RectTransform`의 앵커를 런타임에 조정하며,
화면 회전 등으로 safeArea가 바뀌면 `Update()`에서 자동 재적용한다.

```csharp
using UnityEngine;

/// <summary>
/// RectTransform을 Safe Area에 맞게 조정하여 노치/펀치홀 기기에서 UI가 잘리지 않도록 한다.
/// Canvas 바로 아래 Panel에 부착하여 사용한다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    private RectTransform _rectTransform;
    private Rect _lastSafeArea;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    private void Update()
    {
        if (_lastSafeArea != Screen.safeArea)
            ApplySafeArea();
    }

    /// <summary>
    /// Screen.safeArea를 기반으로 RectTransform의 앵커를 조정한다.
    /// </summary>
    private void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;
        _lastSafeArea = safeArea;

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        _rectTransform.anchorMin = anchorMin;
        _rectTransform.anchorMax = anchorMax;
    }
}
```

---

### 1.3 Android 뒤로가기 버튼 처리

**파일:** `Assets/Scripts/Manager/UIManager.cs`

Android에서 뒤로가기 버튼(`KeyCode.Escape`)이 아무 반응 없는 문제를 해결한다.
열린 UI 패널이 있으면 닫고, 없으면 일시정지를 토글한다.
레벨업/부활/게임오버 등 강제 UI가 열려 있을 때는 무시한다.

```csharp
private void Update()
{
    if (Input.GetKeyDown(KeyCode.Escape))
        OnBackButtonPressed();
}

/// <summary>
/// Android 뒤로가기 버튼 또는 ESC 키 입력 처리.
/// </summary>
private void OnBackButtonPressed()
{
    // 장비창이 열려 있으면 닫기
    if (_equipmentLayer != null && _equipmentLayer.activeSelf)
    {
        HideEquipment();
        return;
    }

    // 강제 UI(레벨업/부활/게임오버)가 열려 있으면 무시
    if (_levelUpLayer != null && _levelUpLayer.activeSelf)
        return;

    if (_aliveLayer != null && _aliveLayer.activeSelf)
        return;

    if (_gameOverLayer != null && _gameOverLayer.activeSelf)
        return;

    // 일시정지 토글
    if (GameManager.Instance.IsPaused)
    {
        var pausePanel = FindAnyObjectByType<UI_PausePanel>();
        if (pausePanel != null)
            pausePanel.OnResumeClicked();
    }
    else
    {
        var pausePanel = FindAnyObjectByType<UI_PausePanel>();
        if (pausePanel != null)
            pausePanel.OnPauseClicked();
    }
}
```

---

### 1.4 리소스 사전 로딩

**파일:** `Assets/Scripts/Manager/GameManager.cs`

모바일에서 첫 프레임 스터터(hitching)를 방지하기 위해 `Awake()` 시점에 주요 리소스를 미리 로드한다.
CSV 데이터, 주요 프리팹, SpriteAtlas를 사전에 메모리에 올려둔다.

```csharp
/// <summary>
/// 모바일 첫 프레임 스터터 방지를 위해 주요 리소스를 미리 로드한다.
/// </summary>
protected override void Awake()
{
    base.Awake();
    PreloadResources();
}

private void PreloadResources()
{
    Resources.Load<TextAsset>("Data/EquipmentData");
    Resources.Load<TextAsset>("Data/UpgradeData");
    Resources.Load<GameObject>("Prefabs/DamageText");
    Resources.Load<GameObject>("Prefabs/LevelUpText");
    Resources.Load<SpriteAtlas>("UI/Icon");
    Resources.Load<SpriteAtlas>("UI/Common");
}
```

---

### 1.5 오디오 포커스 처리

**파일:** `Assets/Scripts/Manager/AudioManager.cs`

전화 수신, 알림 등으로 앱이 백그라운드로 전환될 때 오디오가 계속 재생되는 문제를 해결한다.
`OnApplicationPause`와 `OnApplicationFocus` 두 콜백 모두에서 `AudioListener.pause`를 제어하여
Android/iOS 양쪽에서 안정적으로 동작하도록 한다.

```csharp
/// <summary>
/// 앱이 포커스를 잃으면(전화 수신 등) 오디오를 일시정지하고, 복귀 시 재개한다.
/// </summary>
private void OnApplicationPause(bool pauseStatus)
{
    if (pauseStatus)
    {
        AudioListener.pause = true;
    }
    else
    {
        AudioListener.pause = false;
    }
}

private void OnApplicationFocus(bool hasFocus)
{
    AudioListener.pause = !hasFocus;
}
```

---

### 1.6 Debug.Log 스트립

**대상 파일 (6개):**
- `Assets/Scripts/Manager/StageManager.cs`
- `Assets/Scripts/Utils/SkillIconPool.cs`
- `Assets/Scripts/Manager/SpriteAtlasBinder.cs`
- `Assets/Scripts/Data/EquipmentDatabase.cs`
- `Assets/Scripts/Upgrade/UpgradeDatabase.cs`
- `Assets/Scripts/UI/UI_EquipmentPanel.cs`

모든 `Debug.Log` / `Debug.LogWarning` / `Debug.LogError` 호출을 `#if UNITY_EDITOR` ~ `#endif`로 래핑하여 프로덕션 빌드에서 불필요한 로그 출력으로 인한 성능 저하를 방지한다.

**적용 예시 (각 파일별):**

```csharp
// StageManager.cs
#if UNITY_EDITOR
    Debug.Log("All stages cleared!");
#endif

// SkillIconPool.cs
#if UNITY_EDITOR
    Debug.LogWarning("SkillIconPool: _iconPrefab is not assigned.");
#endif

// SpriteAtlasBinder.cs
#if UNITY_EDITOR
    Debug.LogWarning($"[SpriteAtlasBinder] SpriteAtlas not found: UI/{tag}");
#endif

// EquipmentDatabase.cs
#if UNITY_EDITOR
    Debug.LogError("EquipmentData.csv not found in Resources/Data/");
#endif

// UpgradeDatabase.cs
#if UNITY_EDITOR
    Debug.LogError("[UpgradeDatabase] Data/UpgradeData CSV not found!");
#endif

// UI_EquipmentPanel.cs (6개소 — 대표 1개)
#if UNITY_EDITOR
    Debug.LogWarning("[EquipmentPanel] _playerModelImage is null. Check Inspector.");
#endif
```

---

## 2. 기존에 잘 구현되어 있던 항목

아래 항목들은 이미 프로젝트에 적절하게 설정/구현되어 있어 별도 수정이 필요하지 않았다.

### 2.1 Object Pooling

`ProjectileBase.cs`에서 `ObjectPool.Instance.Get()` / `ObjectPool.Instance.Return()`을 통해 오브젝트 풀링을 사용한다. `Instantiate`/`Destroy` 대신 풀에서 가져오고 반환하여 GC 부하를 줄인다.

```csharp
// ProjectileBase.cs — 풀에서 폭발 이펙트 가져오기
private void Explode()
{
    if (_destroyed) return;
    _destroyed = true;

    if (_explosionPrefab != null)
    {
        GameObject explosion = ObjectPool.Instance.Get(_explosionPrefab);
        explosion.transform.position = transform.position;
        explosion.transform.rotation = Quaternion.identity;
        FireExplosion fe = explosion.GetComponent<FireExplosion>();
        if (fe != null)
            fe.Init(_damage);
    }

    ReturnToPool();
}

// ProjectileBase.cs — 풀로 반환
private void ReturnToPool()
{
    if (_lifeCoroutine != null)
    {
        StopCoroutine(_lifeCoroutine);
        _lifeCoroutine = null;
    }
    ObjectPool.Instance.Return(gameObject);
}
```

### 2.2 persistentDataPath 사용

`SaveManager.cs`에서 `Application.persistentDataPath`를 사용하여 플랫폼별로 안전한 저장 경로를 확보한다.
Android에서는 내부 저장소, iOS에서는 Documents 디렉토리에 자동 매핑된다.

```csharp
// SaveManager.cs
public static class SaveManager
{
    private static string SavePath =>
        System.IO.Path.Combine(Application.persistentDataPath, "save.json");

    public static void Save()
    {
        SaveData data = EquipmentManager.Instance.ToSaveData();
        data.bestStageIndex = _bestStageIndex;
        string json = JsonUtility.ToJson(data, true);
        System.IO.File.WriteAllText(SavePath, json);
    }

    public static void Load()
    {
        if (!System.IO.File.Exists(SavePath)) return;

        string json = System.IO.File.ReadAllText(SavePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        if (data == null) return;

        GoldManager.Instance.SetGold(data.gold);
        _bestStageIndex = data.bestStageIndex;
        EquipmentManager.Instance.LoadFromSave(data);
    }
}
```

### 2.3 터치 조이스틱

`UI_Joystick.cs`에서 `IPointerDownHandler`, `IDragHandler`, `IPointerUpHandler` 인터페이스를 구현하여 터치 기반 조이스틱을 처리한다. `Input.GetAxis` 같은 키보드 전용 API를 사용하지 않으므로 모바일에서 정상 동작한다.

```csharp
// UI_Joystick.cs
public class UI_Joystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        _background.transform.position = eventData.position;
        _cursor.transform.position = eventData.position;
        _touchPos = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 touchDir = (eventData.position - _touchPos);
        // ... 방향 계산 ...
        GameManager.Instance.JoystickDir = moveDir;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _cursor.transform.position = _cursorStartPos;
        _background.transform.position = _backgroundStartPos;
        GameManager.Instance.JoystickDir = Vector2.zero;
    }
}
```

### 2.4 Unity 설정 항목 (코드 변경 없음)

#### 2.4.1 URP + SRP Batcher

**설정 파일:** `Assets/Settings/Mobile_RPAsset.asset`

```yaml
m_UseSRPBatcher: 1
```

CPU 측 렌더 커맨드를 배칭하여 SetPass Call 횟수를 줄인다. 같은 셰이더를 쓰는 오브젝트들의 드로우콜을 하나로 묶어 CPU→GPU 통신 오버헤드를 감소시킨다. 모바일에서 CPU 바운드 프레임 드롭 방지에 직접적 효과.

#### 2.4.2 IL2CPP 백엔드

**설정 파일:** `ProjectSettings/ProjectSettings.asset`

```yaml
scriptingBackend:
  Android: 1   # 0 = Mono, 1 = IL2CPP
```

C# 코드를 C++로 변환 후 네이티브 컴파일한다. Mono 대비 실행 속도 약 1.5~3배 향상, AOT 컴파일로 코드 난독화 효과, ARM64 빌드 필수 조건.

#### 2.4.3 ETC2 텍스처 압축

**설정 파일:** `ProjectSettings/ProjectSettings.asset`

```yaml
m_BuildTargetDefaultTextureCompressionFormat:
  - Android: 03   # 03 = ETC2
```

OpenGL ES 3.0+ 기기에서 GPU가 직접 디코딩하는 압축 포맷. 비압축 대비 메모리 사용량 1/4~1/6 절감, GPU 텍스처 대역폭 감소로 렌더링 성능 향상.

#### 2.4.4 Shader Variant Stripping

**설정 파일:** `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`

```yaml
m_StripRuntimeDebugShaders: 1
m_StripUnusedPostProcessingVariants: 1
m_StripUnusedVariants: 1
m_StripScreenCoordOverrideVariants: 1
```

빌드에 포함되는 셰이더 배리언트 수를 대폭 줄인다. 빌드 크기 감소(수십~수백 MB), 앱 초기 로딩 시간 단축, 런타임 셰이더 컴파일 스터터 방지.

#### 2.4.5 Adaptive Performance

**설정 파일:** `Assets/Settings/Mobile_RPAsset.asset`

```yaml
m_UseAdaptivePerformance: 1
```

Samsung Adaptive Performance SDK와 연동하여 기기의 발열/배터리/GPU 부하 상태를 실시간 모니터링한다. 과열 감지 시 자동으로 해상도/LOD/프레임 타겟을 낮춰 쓰로틀링과 강제 종료를 예방한다.

#### 2.4.6 64-bit ARM64

**설정 파일:** `ProjectSettings/ProjectSettings.asset`

```yaml
AndroidTargetArchitectures: 2   # 2 = ARM64 only
```

2019년 8월부터 Google Play는 64비트 네이티브 라이브러리를 필수로 요구한다. ARM64 빌드는 이 요구사항을 충족하며, 64비트 레지스터/명령어를 활용해 ARMv7 대비 연산 성능이 향상된다.

---

## 3. Unity Editor에서 수동 확인 필요 항목

### 3.1 앱 아이콘

- **어디서:** Project Settings > Player > Icon
- **어떻게:** Default Icon 및 Adaptive Icon(Foreground/Background) 슬롯에 게임 전용 아이콘 이미지를 등록한다.
- **왜:** 기본 Unity 아이콘이 그대로 나가면 앱스토어 심사 거절 사유가 되며, 사용자 인지도에도 영향을 준다.

### 3.2 Android Keystore 서명

- **어디서:** Project Settings > Player > Publishing Settings
- **어떻게:** `Create New Keystore`로 .keystore 파일을 생성하고, Alias/Password를 설정한다. 릴리즈 빌드 시 이 키스토어로 서명한다.
- **왜:** 서명되지 않은 APK/AAB는 Google Play에 업로드할 수 없다. 키스토어 파일은 분실 시 앱 업데이트가 불가능하므로 안전하게 백업해야 한다.

### 3.3 CanvasScaler 설정

- **어디서:** 각 Canvas 오브젝트의 Inspector > Canvas Scaler 컴포넌트
- **어떻게:** UI Scale Mode를 `Scale With Screen Size`로 설정하고, Reference Resolution을 `1080 x 1920`, Match를 `0.5`로 설정한다.
- **왜:** 다양한 해상도/비율의 모바일 기기에서 UI 크기가 일관되게 표시되도록 보장한다.

### 3.4 URP Shadow / MSAA 조정

- **어디서:** URP Asset (`Assets/Settings/Mobile_RPAsset.asset`) Inspector
- **어떻게:** Shadow Resolution을 `1024` 이하로 낮추고, MSAA를 `Disabled` 또는 `2x`로 설정한다.
- **왜:** 모바일에서 고해상도 그림자와 높은 MSAA는 GPU 부담이 크다. 저사양 기기에서 프레임 드롭의 주요 원인이 된다.

### 3.5 SafeArea 컴포넌트 부착

- **어디서:** Canvas 하위의 루트 Panel 오브젝트 Inspector
- **어떻게:** 1.2에서 생성한 `SafeArea.cs` 스크립트를 Canvas 바로 아래의 Panel에 Add Component로 부착한다.
- **왜:** 코드를 작성했더라도 실제 오브젝트에 부착하지 않으면 동작하지 않는다. 모든 주요 UI Canvas에 적용해야 노치/펀치홀 대응이 완료된다.
