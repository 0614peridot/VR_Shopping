# Task 05 — Product Focus and UI

참고: `MDs/00_ProjectStructure.md` (`CS_ProductFocusController`, `CS_FocusUIController`), `MDs/Workflow.md` 섹션 5. 선행: Task 01~04.

## 구현 범위

- `Assets/ShoppingVR/Scripts/CS_ProductFocusController.cs`
- `Assets/ShoppingVR/Scripts/CS_FocusUIController.cs`

## 요구사항

- `UI/Click`은 PassThrough 액션이므로 `performed` + `ReadValueAsButton()`으로 누름/뗌을 구분한다 (`started`는 발생하지 않음).
- 입력 콜백 안에서는 EventSystem 판정이 부정확하므로 콜백은 플래그만 기록하고 `Update`에서 처리한다.
- Product LayerMask만 Raycast, 가장 가까운 `CS_ProductItem` 하나만 선택
- 포커스 중 다른 상품 클릭 시 기존 포커스 정리 후 새 상품 포커스
- 진입 순서: 기존 정리 → 현재 상품 설정 → FocusAnchor 배치·피드백 → UI 표시 → 이동 잠금
- 포커스된 상품 위에서 누른 경우에만 드래그 회전(yaw: 월드 up, pitch: 카메라 right, 도/픽셀). 뗌/취소 시 즉시 정지. UI 위에서는 시작하지 않음
- 담기 버튼: 포커스 상품이 있을 때만 +1, 포커스 유지
- 나가기 버튼과 `UI/Cancel`(Esc)은 같은 `ExitFocus()` 호출
- 종료 순서: 드래그 취소 → 상품 복원 → UI 숨김 → 현재 상품 해제 → 이동 복구 → Explore. 포커스 상태가 아닐 때 호출해도 이동 상태를 건드리지 않는다.
- 포커스 상품이 파괴·비활성화되거나 컨트롤러가 비활성화돼도 UI와 이동 잠금이 남지 않음
- 필수 참조가 없으면 한 번만 오류 로그 후 기능 중단
- 입력 액션은 이 컴포넌트가 켠 경우에만 끈다 (UI 모듈과 공유)
- `Configure(...)` 시그니처와 직렬화 필드 이름은 `CS_ShoppingEnvironmentBuilder` 의존성 때문에 유지
