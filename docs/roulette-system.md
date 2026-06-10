# 룰렛 시스템 (Slot-Machine Style Roulette)

## 1. 시스템 개요

레벨업 시 플레이어에게 3개의 랜덤 업그레이드를 제시하는 **슬롯머신 스타일 룰렛 UI** 시스템이다. 각 슬롯은 수직으로 아이콘이 빠르게 스크롤되다가 순차적으로 멈추며, 최종 결과 아이콘이 중앙에 스냅되는 연출을 제공한다.

**핵심 특징:**
- 3개 슬롯이 순차적으로 정지하는 슬롯머신 연출
- 순환 버퍼 기반 무한 스크롤 (5개 아이콘으로 무한 루프)
- `Time.timeScale = 0` 환경에서 `unscaledDeltaTime`으로 동작
- 탭 2회로 애니메이션 스킵 가능
- 레벨업 큐잉으로 연속 레벨업 처리

---

## 2. 클래스 구조

```
UI_LevelUp           (패널 컨트롤러 - 전체 흐름 관리)
 ├── UI_UpgradeSlot[] (개별 슬롯 - 스핀/정지/스냅 애니메이션)
 └── UpgradeDatabase  (데이터 - CSV 기반 업그레이드 풀 관리)
```

### UI_LevelUp (`Assets/Scripts/UI/UI_LevelUp.cs`)
- **역할:** 레벨업 이벤트 수신, 패널 표시/숨김, 룰렛 타이밍 제어, 업그레이드 적용
- **주요 필드:**
  - `_slots[]` — 3개의 UI_UpgradeSlot 참조
  - `_pendingLevelUps` — 연속 레벨업을 큐로 관리
  - `_spinDuration` / `_stopInterval` — 스핀 지속 시간 및 슬롯 간 정지 간격

### UI_UpgradeSlot (`Assets/Scripts/UI/UI_UpgradeSlot.cs`)
- **역할:** 개별 슬롯의 스트립 생성, 스크롤 애니메이션, 정지 & 스냅
- **주요 필드:**
  - `_strip` — 스크롤되는 아이콘 컨테이너 (RectTransform)
  - `_stripIcons[]` / `_stripInfos[]` — 순환 버퍼로 관리되는 아이콘 목록
  - `_viewport` — RectMask2D 클리핑 영역
  - `STRIP_COUNT = 5` — 순환 버퍼 크기

### UpgradeDatabase (`Assets/Scripts/Upgrade/UpgradeDatabase.cs`)
- **역할:** CSV에서 업그레이드 데이터 로드, 랜덤 선택 (maxLevel 필터링)
- `PickRandom(3)` — 이미 만렙인 업그레이드를 제외하고 3개 랜덤 추출

---

## 3. 전체 동작 흐름

```
ExpManager.OnLevelUp(level)
    │
    ▼
UI_LevelUp.OnLevelUp()
    │  _pendingLevelUps.Enqueue(level)
    │  WaitForSeconds(_delayShowPanel)
    ▼
UI_LevelUp.Show(level)
    │  Time.timeScale = 0
    │  UpgradeDatabase.PickRandom(3) → 3개 업그레이드 선택
    │  각 슬롯: SetUpgrade(info) + StartSpin()
    │  RouletteStopCoroutine() 시작
    ▼
RouletteStopCoroutine()
    │  WaitForSecondsRealtime(_spinDuration=1s)  ← 전체 스핀 시간
    │  슬롯[0].StopSpin() → 감속 정지
    │  WaitForSecondsRealtime(_stopInterval=0.5s)
    │  슬롯[1].StopSpin() → 감속 정지
    │  WaitForSecondsRealtime(_stopInterval=0.5s)
    │  슬롯[2].StopSpin() → 감속 정지
    │  모든 슬롯 SetInteractable(true)
    ▼
사용자가 슬롯 선택 → OnSlotSelected(idx)
    │  PlayerUpgrade.AddUpgrade(type)
    │  PlayerController.ApplyUpgradeEffect(type)
    │  _pendingLevelUps가 남아있으면 Show() 재호출
    │  없으면 Close() → Time.timeScale = 1
    ▼
게임 재개
```

---

## 4. 핵심 메커니즘 상세

### 4.1 SetupStrip() — 런타임 UI 생성 + 순환 스트립

슬롯머신 효과를 위해 **런타임에 뷰포트와 스트립을 동적 생성**한다.

```
┌─────────────────────────────────────┐
│  원본 아이콘 위치에 IconViewport 생성  │
│  (RectMask2D로 클리핑 영역 확보)      │
│                                     │
│  ┌─ IconViewport (200×200) ──┐      │
│  │  ┌─ Strip ──────────┐     │      │
│  │  │  Icon_4  (맨 위)  │     │      │
│  │  │  Icon_3          │     │      │
│  │  │ [Icon_2] ← 보임  │     │      │
│  │  │  Icon_1          │     │      │
│  │  │  Icon_0  (맨 아래)│     │      │
│  │  └──────────────────┘     │      │
│  └───────────────────────────┘      │
└─────────────────────────────────────┘
```

**핵심 코드 설명:**

```csharp
// 1) 원본 아이콘과 동일한 위치/크기로 뷰포트 생성 (RectMask2D가 클리핑 담당)
_iconViewportGo = new GameObject("IconViewport", typeof(RectTransform), typeof(RectMask2D));

// 2) 원본 아이콘을 뷰포트 자식으로 이동 후 숨김
iconRT.SetParent(vpRT, false);
_iconImage.enabled = false;

// 3) Strip 컨테이너: 앵커를 하단에 고정, 높이 = iconHeight × STRIP_COUNT
_strip.anchorMin = new Vector2(0, 0);
_strip.anchorMax = new Vector2(1, 0);
_strip.sizeDelta = new Vector2(0, _iconHeight * STRIP_COUNT);

// 4) 5개 아이콘을 등간격으로 배치 (랜덤 스프라이트)
rt.anchoredPosition = new Vector2(0, _iconHeight * i + _iconHeight * 0.5f);
```

- 원본 `_iconImage`의 부모/위치를 저장해두고, 스핀 종료 후 `CleanupStrip()`에서 복원
- `STRIP_COUNT = 5`개만으로 무한 스크롤을 구현 (순환 재활용)

### 4.2 ScrollCoroutine() — 무한 스크롤 + 재활용 + 정지 로직

매 프레임 Strip을 아래로 이동시키고, 뷰포트 밖으로 나간 아이콘을 상단으로 재배치한다.

```
프레임마다:
  ┌──────────────────────────────────────────────┐
  │  1. Strip.y -= speed * unscaledDeltaTime     │
  │                                              │
  │  2. 각 아이콘의 worldY 검사                    │
  │     if (worldY < -iconHeight/2):             │
  │       → 최상단 아이콘 위로 재배치               │
  │       → 새 랜덤 스프라이트 할당                 │
  │                                              │
  │  3. 중앙 아이콘 변경 감지 → 사운드 + 이름 표시  │
  │                                              │
  │  4. _stopping == true이면:                    │
  │     → 최종 아이콘(assigned)을 다음 위치에 삽입  │
  │     → 중앙 근처 도달 시 SnapToCenter() 호출    │
  └──────────────────────────────────────────────┘
```

**순환 재활용 로직:**

```csharp
// 하단 밖으로 나간 아이콘을 최상단으로 이동
if (worldY < -_iconHeight * 0.5f)
{
    // 현재 스트립에서 가장 높은 y값 찾기
    float maxY = float.MinValue;
    for (int j = 0; j < _stripIcons.Count; j++)
        if (j != i && _stripIcons[j].rectTransform.anchoredPosition.y > maxY)
            maxY = _stripIcons[j].rectTransform.anchoredPosition.y;

    iconPos.y = maxY + _iconHeight;  // 최상단 바로 위에 배치
    rt.anchoredPosition = iconPos;

    // 정지 중이 아니면 새 랜덤 스프라이트 할당
    if (!_finalIconPlaced)
    {
        UpgradeInfo randInfo = all[Random.Range(0, all.Length)];
        _stripIcons[i].sprite = randInfo.icon;
    }
}
```

**정지 시 최종 아이콘 삽입:**

```csharp
if (_stopping && !_finalIconPlaced)
{
    // 중앙에 가장 가까운 아이콘의 "다음" 위치에 최종 결과를 삽입
    int closestIdx = GetClosestIconToCenter(centerY);
    int nextIdx = (closestIdx + 1) % STRIP_COUNT;
    _stripIcons[nextIdx].sprite = _assigned.icon;
    _stripInfos[nextIdx] = _assigned;
    _finalIconPlaced = true;
}
```

### 4.3 SnapToCenter() — ease-out 보간 스냅

최종 아이콘이 중앙 근처에 도달하면, **ease-out quadratic 보간**으로 정확히 중앙에 정렬한다.

```csharp
private IEnumerator SnapToCenter(float centerY)
{
    // 최종 아이콘의 로컬 Y를 기반으로 Strip의 목표 위치 계산
    float currentIconLocalY = _stripIcons[targetIdx].rectTransform.anchoredPosition.y;
    float targetStripY = centerY - currentIconLocalY;

    Vector2 startPos = _strip.anchoredPosition;
    Vector2 endPos = new Vector2(startPos.x, targetStripY);

    float duration = 0.15f;
    float t = 0f;
    while (t < duration)
    {
        t += Time.unscaledDeltaTime;
        // ease-out quadratic: 빠르게 시작 → 부드럽게 감속
        float ease = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / duration), 2f);
        _strip.anchoredPosition = Vector2.Lerp(startPos, endPos, ease);
        yield return null;
    }
    _strip.anchoredPosition = endPos;
}
```

- `duration = 0.15s`로 짧고 날카로운 스냅 느낌
- ease-out 커브: `1 - (1-t)^2` — 처음에 빠르게 이동하고 끝에서 부드럽게 정지

### 4.4 StopSpin() — 즉시/감속 정지 분기

```csharp
public void StopSpin(bool playConfirmSound = true)
{
    if (!playConfirmSound)
    {
        // 즉시 정지 (스킵 모드): 코루틴 중단 → 스트립 제거 → 최종 결과 표시
        StopCoroutine(_spinCoroutine);
        CleanupStrip();
        Display(_assigned);
        StartCoroutine(PunchScale());  // 바운스 피드백
        return;
    }

    // 감속 정지: _stopping 플래그만 설정 → ScrollCoroutine에서 처리
    _stopping = true;
}
```

| 모드 | 트리거 | 동작 |
|------|--------|------|
| **감속 정지** | `RouletteStopCoroutine`에서 순차 호출 | `_stopping = true` → 최종 아이콘 삽입 → 중앙 스냅 |
| **즉시 정지** | 사용자 탭 2회 (스킵) | 코루틴 즉시 중단 → 스트립 제거 → 결과 직접 표시 |

---

## 5. 설계 포인트

### 순환 버퍼 (Circular Buffer)
- 5개 아이콘(`STRIP_COUNT`)만으로 무한 스크롤을 구현
- 뷰포트 하단을 벗어난 아이콘을 최상단으로 재배치하며 새 스프라이트 할당
- 오브젝트 생성/파괴 없이 메모리 효율적

### unscaledDeltaTime
- 레벨업 시 `Time.timeScale = 0`으로 게임을 일시정지
- 룰렛 애니메이션은 `Time.unscaledDeltaTime`과 `WaitForSecondsRealtime`으로 독립 동작
- 게임 일시정지 중에도 UI 애니메이션이 매끄럽게 재생

### 관심사 분리
| 클래스 | 책임 |
|--------|------|
| `UI_LevelUp` | 이벤트 수신, 타이밍 제어, 업그레이드 적용 |
| `UI_UpgradeSlot` | 개별 슬롯 애니메이션 (스핀/정지/스냅) |
| `UpgradeDatabase` | 데이터 로드, 풀 필터링, 랜덤 추출 |

### 레벨업 큐잉
- 짧은 시간에 여러 레벨업이 발생하면 `Queue<int>`에 쌓임
- 하나의 선택이 끝나면 큐에서 다음 레벨업을 꺼내 즉시 `Show()` 재호출
- 레벨업이 누락되지 않음

### 탭 스킵
- 패널 영역에 투명 Button을 동적 추가하여 탭 감지
- 1탭: 무시, 2탭: `SkipAnimation()` → 모든 슬롯 즉시 정지
- 룰렛 연출을 보고 싶은 플레이어와 빠르게 넘기고 싶은 플레이어 모두 만족

### 런타임 UI 생성 & 복원
- `SetupStrip()`에서 뷰포트/스트립을 동적 생성하고, 원본 아이콘의 부모/위치를 저장
- `CleanupStrip()`에서 동적 오브젝트를 Destroy하고 원본 UI를 원래 상태로 복원
- 프리팹 구조를 오염시키지 않으면서 복잡한 애니메이션을 구현
