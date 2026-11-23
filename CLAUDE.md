# CLAUDE.md

이 프로젝트에서 코드를 작성하거나 수정할 때 반드시 따를 규칙.

## 1. 폴더 구조

### MVP 구조

- asmdef를 이용하여 각 계층(Model / View / Presenter)을 물리적으로 분리한다.
- Model / View / Presenter 폴더는 `Assets/_Project/Scripts` 안에 둔다.
- 에디터 관련 스크립트는 `_Project_Editor`에 분리 저장한다.

#### Model
- 모든 데이터를 총괄한다.

#### View
- 화면 표시와 입력 수집만 담당한다.

#### Presenter
- Model과 View를 연결하고 게임 진행 로직을 담당한다.

### namespace 규정

`Anomaly.<레이어>.<하위폴더>` 로 규정한다.

예: `Anomaly.Model.Anomalies`, `Anomaly.View.Player`, `Anomaly.Presenter.Loop`

### 직렬화 규칙

- 인스펙터 노출 필드는 **`[SerializeField] private`**. `public` 필드 금지.
- 다른 클래스가 읽어야 하면 읽기 전용 프로퍼티로 연다.

```csharp
[SerializeField] private float moveSpeed = 3f;
public float MoveSpeed => moveSpeed;
```

### asmdef와 참조 방향

| asmdef | 위치 | 참조 가능 |
| --- | --- | --- |
| `Anomaly.Model` | `Assets/_Project/Scripts/Model` | 없음 |
| `Anomaly.View` | `Assets/_Project/Scripts/View` | 없음 (Model 참조 금지) |
| `Anomaly.Presenter` | `Assets/_Project/Scripts/Presenter` | `Anomaly.Model`, `Anomaly.View` |
| `Anomaly.Editor` | `Assets/_Project_Editor` | 전부. Editor 플랫폼 전용 |

- 위 방향을 거스르는 참조를 추가하지 않는다.

## 2. 계층 간 통신

- View는 입력과 상호작용을 C# 이벤트(`event Action<...>`)로 알린다. View가 Presenter나 Model을 직접 호출하지 않는다.
- Presenter가 View의 이벤트를 구독하고, Model을 갱신한 뒤 View에 표시를 지시한다.
- `FindObjectOfType`, `FindObjectsByType`, `GameObject.Find`, 싱글톤 등 전역 검색·전역 접근은 금지한다.
  - 예외: 게임 규칙과 무관하고 게임 상태를 갖지 않는 공용 서비스(사운드 재생, 로그 등)는 싱글톤을 허용한다.
  - 예외에 해당해도 맵 진행, 판정, 이상현상, 플레이어 상태 같은 게임 상태를 그 안에 두지 않는다.
  - 에디터 전용 스크립트(`Assets/_Project_Editor`)는 이 규칙의 대상이 아니다.
- 의존성은 씬마다 진입점 MonoBehaviour 하나(예: `GameManager`)에서 생성·연결한다.

## 3. 데이터

- 이상현상 하나는 ScriptableObject 데이터 에셋 하나로 정의한다(ID, 난이도, 방, 종류: 색상 변경 / 모델 교체 / 직접 제작).
- 게임 규칙 숫자(1~7번 맵 이상현상 등장 확률 50%, 목표 맵 8번, 0번 맵은 항상 정상 등)는 설정 ScriptableObject 하나에 모은다. 코드에 하드코딩하지 않는다.
- 데이터 에셋은 `Assets/_Project/Data` 아래에 둔다.

## 4. 외부 에셋

- `Assets/KayKit_Furniture_Bits_1.0_FREE`, `Assets/Haons SD series Pack` 안의 파일은 직접 수정하지 않는다.
- 바꿀 것이 있으면 `Assets/_Project` 쪽에 복사하거나 머티리얼·프리팹 변형으로 처리한다.
- HAON 폴더는 Git에 올라가지 않으므로, 그 안에서 고친 내용은 다른 PC에서 사라진다.

## 5. 생성된 씬

- `Assets/Scenes/Level_Corridor.unity`는 메뉴 **Anomaly > Build Level Layout**(`LevelLayoutBuilder.cs`)이 실행할 때마다 새로 만들어 덮어쓴다.
- 배치를 바꿀 때는 씬을 직접 고치지 말고 `LevelLayoutBuilder.cs`를 고친 뒤 다시 실행한다.
- `Assets/Scenes/Lobby.unity`도 같은 방식이다. 메뉴 **Anomaly > Build Lobby**(`LobbyBuilder.cs`)가 만들고, Build Profiles 씬 목록을 Lobby → Level_Corridor 순서로 맞춘다.

## 6. 입력

- 입력은 Input System(1.18)만 사용한다. `Assets/InputSystem_Actions.inputactions`의 액션을 쓴다.
- 옛 Input Manager API(`Input.GetKey`, `Input.GetAxis` 등)는 쓰지 않는다.

## 7. 엔진·렌더링

- Unity 6000.3.8f1, Universal Render Pipeline(17.3) 기준이다.
- 머티리얼은 URP 셰이더(`Universal Render Pipeline/Lit` 등)만 쓴다. Built-in 셰이더(`Standard` 등) 금지.

## 8. Git

- `.meta` 파일은 항상 해당 에셋과 함께 커밋한다.
- `Assets/Haons SD series Pack/`은 절대 커밋하지 않는다(Unity Asset Store EULA / Unity-Chan License).
- `Library/`, `Temp/`, `Logs/`, `UserSettings/`, IDE 파일은 `.gitignore`로 제외되어 있다.

## 9. 기획 기준 문서

- 게임 규칙, 맵 구성, 이상현상 풀 등 기획 내용은 기획서를 기준으로 한다: https://claude.ai/artifact/JuGRZeP1x3Y5UjGAzUF7BN
- 기획서와 코드가 다르면 임의로 맞추지 말고 먼저 확인한다.
