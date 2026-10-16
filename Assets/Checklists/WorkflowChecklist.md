# ShoppingVR Workflow Checklist

규칙: 실제 확인한 항목만 `[x]`. 진행 불가 시 `BLOCKED: 이유`. 헤드셋 없이 확인한 항목은 `Editor verified`.

## Task 01 — Project Audit and Mode

- [x] Unity `6000.6.4f1` 확인 — ProjectVersion.txt `6000.6.4f1 (12bfff696524)` 확인
- [x] URP `17.6.0`, Input System `1.20.0`, uGUI `2.6.0` 확인 — manifest.json / packages-lock.json 모두 일치
- [x] Active Input Handling = 새 Input System — `activeInputHandler: 1` 확인
- [x] `Player/Move` W/A/S/D + 방향키 바인딩 — `WASD` 2DVector composite에 `<Keyboard>/w,a,s,d` + `upArrow/downArrow/leftArrow/rightArrow` 확인
- [x] `UI/Point` 마우스 위치 — `<Mouse>/position` 확인
- [x] `UI/Click` 마우스 왼쪽 버튼 — `<Mouse>/leftButton` 확인
- [x] `UI/Cancel` Esc 포함 — `*/{Cancel}` usage 바인딩, Keyboard `escape` 컨트롤 usages에 `Cancel` 포함 확인 (명시적 `<Keyboard>/escape` 경로는 없음)
- [x] XR 패키지 상태 기록 (미설치 시 제한사항) — XR Interaction Toolkit / XR Plug-in Management / OpenXR 미설치 (내장 `com.unity.modules.xr`만 존재). 설치하지 않음, 키보드·마우스로만 검증 (device verification required)
- [x] `CS_ShoppingMode.cs` 생성 (`Explore`, `Focused`) — `.meta`는 Unity 생성 대기
- [x] Unity 컴파일 오류 0 — 08:40 Editor 자동 컴파일 `Tundra build success`, `error CS` 없음 (Logs/Editor.log 확인)

## Task 02 — Player Locomotion

- [x] `CS_PlayerLocomotion.cs` 코드 작성 (외부 도구 초안을 명세에 맞게 수정)
- [x] RequireComponent, 카메라 기준 수평 이동, m/s 속도 + deltaTime, `SetMovementEnabled`
- [x] OnEnable/OnDisable 대칭 (자신이 켠 액션만 끔), 필수 참조 누락 시 1회 오류
- [x] `lookSensitivity`의 `Min(1)` 오류 수정 (기본값 0.08이 1로 강제되던 문제)
- [x] 오프라인 컴파일 검사 통과 (Unity 6000.6.4f1 Roslyn + 프로젝트 참조 DLL, 오류 0) — Unity Console 확인 아님
- [x] Unity 컴파일 오류 0 — 08:40 Editor 자동 컴파일 `Tundra build success`, `error CS` 없음 (Logs/Editor.log 확인)
- [x] 레거시 `UnityEngine.Input` 미사용 (grep 확인)
- [ ] Play Mode 이동 검증 (Task 06)

## Task 03 — Product Item

- [x] `CS_ProductItem.cs` 코드 작성 (외부 도구 초안을 명세에 맞게 수정)
- [x] 진열 상태 저장/복원 (부모, 로컬 Transform, Rigidbody, Collider trigger)
- [x] 포커스 시 크기 강제(1,1,1) 제거 → 월드 크기 유지
- [x] MaterialPropertyBlock 피드백, 비활성화 시 피드백 제거
- [x] 오프라인 컴파일 검사 통과 (Unity 6000.6.4f1 Roslyn + 프로젝트 참조 DLL, 오류 0) — Unity Console 확인 아님
- [x] Unity 컴파일 오류 0 — 08:40 Editor 자동 컴파일 `Tundra build success`, `error CS` 없음 (Logs/Editor.log 확인)
- [x] 레거시 `UnityEngine.Input` 미사용 (grep 확인)
- [ ] Play Mode 복원 검증 (Task 06)

## Task 04 — Cart Counter

- [x] `CS_CartCounter.cs` 코드 작성 (초기 0, +1, 즉시 배지 갱신, 0일 때 숨김 옵션)
- [x] 오프라인 컴파일 검사 통과 (Unity 6000.6.4f1 Roslyn + 프로젝트 참조 DLL, 오류 0) — Unity Console 확인 아님
- [x] Unity 컴파일 오류 0 — 08:40 Editor 자동 컴파일 `Tundra build success`, `error CS` 없음 (Logs/Editor.log 확인)
- [x] 레거시 `UnityEngine.Input` 미사용 (grep 확인)
- [ ] Play Mode 배지 증가 검증 (Task 06)

## Task 05 — Product Focus and UI

- [x] `CS_ProductFocusController.cs`, `CS_FocusUIController.cs` 코드 작성 (외부 도구 초안을 명세에 맞게 수정)
- [x] Click(PassThrough) 처리를 `performed`+버튼 값으로 수정 (기존 `started` 구독은 호출되지 않음)
- [x] UI 위 판정을 입력 콜백 밖(Update)으로 이동
- [x] 포커스 중 다른 상품 클릭 시 전환, 상품 비활성화 시 종료, 컨트롤러 비활성화 시 정리
- [x] 진입/종료 순서 명세대로, 비포커스 상태에서 ExitFocus가 이동 상태를 바꾸지 않음
- [x] UI 버튼 listener 대칭 등록/해제, 가격 `N0` + `원`
- [x] 오프라인 컴파일 검사 통과 (Unity 6000.6.4f1 Roslyn + 프로젝트 참조 DLL, 오류 0) — Unity Console 확인 아님
- [x] Unity 컴파일 오류 0 — 08:40 Editor 자동 컴파일 `Tundra build success`, `error CS` 없음 (Logs/Editor.log 확인)
- [x] 레거시 `UnityEngine.Input` 미사용 (grep 확인)
- [ ] Play Mode 선택·회전·담기·나가기 검증 (Task 06)

## Task 06 — Integration Validation

- [ ] (Task 06 문서 작성 후 채움)

## Device Validation

- [ ] device verification required
