# Anomaly my room

> 오늘도 즐거운 나만의 집으로 향하는 여정! 하지만 집에 이상한 기시감이 드는데...?

![대표 스크린샷](Docs/screenshot.png)
<!-- 스크린샷 또는 플레이 GIF 추가 예정 -->

## 게임 소개

| 항목 | 내용 |
| --- | --- |
| 장르 | 이상현상 찾기(8번 출구 라이크), 관찰·추리 |
| 시점 | 3D 탑뷰, 물건 조사 시 클로즈업 카메라 |

8번 출구 형식의 이상현상 찾기 게임. 3D 탑뷰 시점으로 복도와 방을 돌아다니며 평소와 달라진 점을 찾아야합니다.

### 플레이 루프

1. 로비(엘리베이터)에서 게임 시작 → 인트로 스토리(처음 한 번) → 0번 맵에서 시작
2. 0번 맵은 항상 정상이며, 평소 모습을 기억하는 기준이 됨
3. 복도와 방을 돌며 가구를 살펴보고, 필요하면 물건을 조사함(클로즈업)
4. 판단에 따라 행동함
   - 이상현상이 없다 → 103호 침대 주변(발치·양옆)에서 상호작용 → 잠들기
   - 이상현상이 있다 → 복도 서쪽 끝 출구 지역(주황색 표시)에 들어감 → 나가기
5. 맞히면 다음 맵, 틀리면 0번 맵으로 돌아감. 전환 뒤 결과 문구가 뜸
   - 잠들기로 맞힘: "역시 내 집이 맞았어"
   - 나가기로 맞힘: "역시 내 집이 아니었어"
   - 틀림: "분명 맞는거 같았는데..." 

### 클리어 조건

- **클리어**: 7번 맵에서 맞혀 8번 맵에 도달 → 엔딩 화면, 로비로 돌아갈 수 있음
- **실패시**: 0번 맵으로 돌아감(게임 오버 없음)

### 이상현상

- 1~7번 맵에서 50% 확률로 하나가 발생함
- 풀에서 무작위로 뽑으며, 풀을 다 쓰기 전까지 같은 이상현상은 다시 나오지 않음(0번 맵으로 돌아가도 초기화하지 않음)
- 현재 54개 구현: 복도 바닥 타일·조명 색 변경 2개, 가구 색상 변경 32개, 비슷한 모양의 다른 가구로 교체 20개
- 조사해야만 알 수 있는 고난이도 이상현상 5개는 추가 예정(기획 기준 총 59개)

## 조작법

| 동작 | 키보드·마우스 | 게임패드 |
| --- | --- | --- |
| 이동 | WASD / 방향키 | 왼쪽 스틱 |
| 조사 / 잠들기 | E | Y (North) |
| 조사 화면 닫기 | E / Esc | Y / B (East) |
| 팝업 선택 | 마우스 클릭, 방향키 + Enter | 스틱 + A |
| 팝업 취소 | Esc | B |
| 일시정지 메뉴 | Esc | B |
| 인트로 스토리 넘기기 | 클릭 / Enter | A |

화면 아래 안내 문구("E  잠들기" / "E  조사: 액자")로 지금 E를 누르면 무엇이 실행될지 보여줌. 여러 대상이 겹치면 가장 가까운 것이 선택됨.

### 실행

- 에디터: HAON SD 캐릭터 팩을 임포트한 뒤 메뉴 **Anomaly > Build Level Layout**, **Anomaly > Build Lobby** 실행 → `Assets/Scenes/Lobby.unity`에서 플레이
- 빌드: 준비 중
- 웹 / 스토어: 준비 중

## 주요 기능

| 기능 | 상태 |
| --- | --- |
| 레벨·로비 자동 배치(에디터 메뉴 Anomaly > Build Level Layout / Build Lobby) | 구현 |
| 플레이어 이동·대기/걷기 애니메이션, 발소리, 탑뷰 카메라 | 구현 |
| 맵 진행·판정(침대 주변 잠들기, 서쪽 출구 지역 나가기, 확인 팝업, 전환 중 잠금) | 구현 |
| 이상현상 시스템(풀 54개, 다 쓰면 다시 채움) | 구현 |
| 물건 조사(클로즈업 카메라, 이름 표시) | 구현 |
| 방 입구 벽 투명 처리(카메라 레이캐스트) | 구현 |
| 맵 전환 연출(잠들기·나가기 페이드 이미지, 결과 문구, 맵 번호 푯말, 정답·오답 효과음) | 구현 |
| 로비(엘리베이터), 인트로 스토리, 일시정지 메뉴, 엔딩 후 로비 복귀, 크레딧 | 구현 |
| 디버그 도구(Anomaly > Debug Window) | 구현 |
| 고난이도 이상현상 5개, 조사 설명 문구 | 예정 |
| 실행 파일 빌드 | 예정 |

전투, 인벤토리, 세이브/로드, 멀티플레이, AI 적은 이 게임에 없음.

### 개발 도구(에디터 메뉴 Anomaly)

| 메뉴 | 내용 |
| --- | --- |
| Build Level Layout | 게임 씬(`Level_Corridor`)을 새로 만듦. 배치·이상현상·UI 연결까지 자동 |
| Build Lobby | 로비 씬(`Lobby`)을 새로 만들고 씬 목록을 Lobby → Level_Corridor 순서로 맞춤 |
| Build Player Animator / Build Lobby Animator | 플레이어(대기·걷기), 로비(서 있기) 애니메이터 생성 |
| Render Leave Image | 나가기 페이드 이미지(캐릭터 렌더링) 생성 |
| Debug Window | 플레이 중 맵 이동, 이상현상 지정·클로즈업, 정답/오답 처리 |
| Reset Intro Story Seen | 인트로 스토리를 다시 보이게 함 |

씬은 메뉴가 만들어 덮어쓰므로 직접 고치지 않고 빌더 스크립트를 고친 뒤 다시 실행함.

## 개발 환경 및 기술 스택

| 항목 | 내용 |
| --- | --- |
| 엔진 | Unity 6000.3.8f1 |
| 렌더 파이프라인 | Universal Render Pipeline 17.3.0 |
| 언어 | C# |
| 입력 | Input System 1.18.0 |
| 구조 | MVP(Model / View / Presenter, asmdef로 분리), 진입점 `GameManager` / `LobbyManager` |
| 패키지 | Unity Chan Spring Bone 1.2.0-preview |
| 에셋 | KayKit Furniture Bits 1.0, Haon SD series Free Bundle, Kenney RPG Audio, Kenney Interface Sounds |

## 주의 사항

- 이 프로젝트는 포트폴리오용 프로젝트로, 무단 배포를 허용하지 않음.
- 이 저장소에는 에셋 스토어 에셋인 HAON SD 캐릭터가 들어 있지 않음(라이선스상 재배포 불가).

## 사용 에셋 및 라이선스

| 에셋 | 라이선스 | 저장소 포함 |
| --- | --- | --- |
| [KayKit Furniture Bits 1.0](https://kaylousberg.itch.io/furniture-bits) — Kay Lousberg | CC0 | 포함 |
| [Haon SD series Free Bundle](https://assetstore.unity.com/packages/3d/characters/humanoids/haon-sd-series-free-bundle-84992) — 캐릭터 | Unity-Chan License (UCL) | 미포함 |
| Haon SD series Free Bundle — 캐릭터 외 | Unity Asset Store EULA | 미포함 |
| Unity Chan Spring Bone (`Packages/com.unity.springbone`) | MIT | 포함 |
| [Kenney RPG Audio](https://kenney.nl/assets/rpg-audio) — 발소리 | CC0 | 포함 |
| [Kenney Interface Sounds](https://kenney.nl/assets/interface-sounds) — 정답·오답 효과음 | CC0 | 포함 |
| 나가기 페이드 이미지(`Fade_Leave.png`) — HAON 캐릭터 렌더링 실루엣 | Unity-Chan License (UCL) | 포함 |

This work is provided under Unity-Chan License Terms. © Unity Technologies Japan/UCL

해당 프로젝트는 상업적 배포를 진행하지 않습니다.
