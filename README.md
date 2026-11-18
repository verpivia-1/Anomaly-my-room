# Anomaly my room

8번 출구 형식의 이상현상 찾기 게임(3D 탑뷰). 일직선 복도와 방 3개를 돌며 이상현상을 찾고, 이상현상이 없으면 103호 침대에서 상호작용, 있으면 복도 서쪽 끝으로 나가 8번 맵까지 도달하면 클리어.

## 개발 환경

- Unity 6000.3.8f1
- Universal Render Pipeline (URP)

## 처음 열 때

이 저장소에는 에셋 스토어 에셋인 HAON SD 캐릭터가 들어 있지 않음(라이선스상 재배포 불가). 프로젝트를 열기 전에 아래를 직접 받아야 함.

1. Unity Asset Store에서 [Haon SD series Free Bundle](https://assetstore.unity.com/packages/3d/characters/humanoids/haon-sd-series-free-bundle-84992)을 받음
2. Package Manager > My Assets에서 임포트해 `Assets/Haons SD series Pack/` 위치에 둠
3. 메뉴 **Anomaly > Build Level Layout**으로 레벨 씬(`Assets/Scenes/Level_Corridor.unity`)을 생성

## 사용 에셋 및 라이선스

| 에셋 | 라이선스 | 저장소 포함 |
| --- | --- | --- |
| [KayKit Furniture Bits 1.0](https://kaylousberg.itch.io/furniture-bits) — Kay Lousberg | CC0 | 포함 |
| [Haon SD series Free Bundle](https://assetstore.unity.com/packages/3d/characters/humanoids/haon-sd-series-free-bundle-84992) — 캐릭터 | Unity-Chan License (UCL) | 미포함 |
| Haon SD series Free Bundle — 캐릭터 외 | Unity Asset Store EULA | 미포함 |
| Unity Chan Spring Bone (`Packages/com.unity.springbone`) | MIT | 포함 |

This work is provided under Unity-Chan License Terms. © Unity Technologies Japan/UCL
