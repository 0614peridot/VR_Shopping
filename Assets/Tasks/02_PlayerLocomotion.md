# Task 02 — Player Locomotion

참고: `MDs/00_ProjectStructure.md` (`CS_PlayerLocomotion`), `MDs/Workflow.md` 섹션 2. 선행: Task 01.

## 구현 범위

- `Assets/ShoppingVR/Scripts/CS_PlayerLocomotion.cs`만 수정

## 요구사항

- `[RequireComponent(typeof(CharacterController))]`
- Camera 수평 전방·오른쪽 + `Player/Move`, 미터/초 `moveSpeed`, `Time.deltaTime`, `CharacterController.Move`
- `SetMovementEnabled(bool)` / `MovementEnabled`로 포커스 중 이동 잠금
- 입력 액션 활성화/비활성화는 `OnEnable`/`OnDisable`에서 대칭. 다른 시스템(UI 모듈)이 이미 켠 액션은 끄지 않는다.
- 필수 참조(Move, View Transform)가 없으면 한 번만 오류 로그 후 이동 중단
- 점프, 달리기, 복잡한 중력 없음 (바닥 붙임용 고정 하강 속도만)

## 기존 코드 관련 메모

- 외부 도구가 만든 선택 기능(오른쪽 마우스 드래그 시점 회전: `lookAction`, `rightClickAction`)이 포함되어 있다. 명세 밖이지만 `CS_ShoppingEnvironmentBuilder`가 이 필드에 의존하므로 유지하고, 참조가 없으면 동작하지 않는 선택 기능으로 둔다.
- 직렬화 필드 이름은 Builder가 `SerializedObject`로 참조하므로 바꾸지 않는다.
