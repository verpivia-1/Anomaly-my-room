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

1. 0번 맵에서 시작. 0번 맵은 항상 정상이며, 평소 모습을 기억하는 기준이 됨
2. 복도와 방을 돌며 가구를 살펴보고, 필요하면 물건을 조사함
3. 판단에 따라 행동함
   - 이상현상이 없다 → 103호 침대에서 상호작용
   - 이상현상이 있다 → 복도 서쪽 끝(주황색 표시)으로 나감
4. 맞히면 다음 맵, 틀리면 0번 맵으로 돌아감

### 클리어 조건

- **클리어**: 8번 맵에 도달
- **실패시**: 0번 맵으로 돌아감(게임 오버 없음)

### 이상현상

- 1~7번 맵에서 50% 확률로 하나가 발생함
- 전체 61개 중 무작위로 뽑으며, 한 판 안에서는 같은 이상현상이 다시 나오지 않음
- 종류: 복도 바닥 타일·조명 색 변경, 가구 색상 변경, 비슷한 모양의 다른 가구로 교체, 조사해야만 알 수 있는 고난이도 이상현상

## 조작법

| 동작 | 키보드·마우스 | 게임패드 |
| --- | --- | --- |
| 이동 | WASD / 방향키 | 왼쪽 스틱 |
| 조사 / 상호작용 | E | A (South) |
| 조사 화면 닫기 | E / Esc | B (East) |

### 실행

- 빌드: 준비 중
- 웹 / 스토어: 준비 중

## 주요 기능

| 기능 | 상태 |
| --- | --- |
| 레벨 자동 배치(에디터 메뉴 Anomaly > Build Level Layout) | 구현 |
| 플레이어 이동, 탑뷰 카메라 | 예정 |
| 물건 조사(클로즈업 카메라 전환, 설명 UI) | 예정 |
| 맵 진행·판정(침대 상호작용, 서쪽 출구) | 예정 |
| 이상현상 시스템(셔플 백 61개) | 예정 |
| 방 입구 벽 투명 처리 | 예정 |

전투, 인벤토리, 세이브/로드, 멀티플레이, AI 적은 이 게임에 없음.

## 개발 환경 및 기술 스택

| 항목 | 내용 |
| --- | --- |
| 엔진 | Unity 6000.3.8f1 |
| 렌더 파이프라인 | Universal Render Pipeline 17.3.0 |
| 언어 | C# |
| 입력 | Input System 1.18.0 |
| 패키지 | Unity Chan Spring Bone 1.2.0-preview |
| 에셋 | KayKit Furniture Bits 1.0, Haon SD series Free Bundle |

## 주의 사항

이 저장소에는 에셋 스토어 에셋인 HAON SD 캐릭터가 들어 있지 않음(라이선스상 재배포 불가).

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
