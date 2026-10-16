# Task 03 — Product Item

참고: `MDs/00_ProjectStructure.md` (`CS_ProductItem`), `MDs/Workflow.md` 섹션 3. 선행: Task 01.

## 구현 범위

- `Assets/ShoppingVR/Scripts/CS_ProductItem.cs`만 수정

## 요구사항

- 상품명, 원 단위 가격, Collider, Renderer를 Inspector에서 설정 (`Configure(...)`는 Builder용으로 유지)
- 포커스 전 부모, 로컬 위치·회전·크기, Rigidbody(isKinematic/useGravity), Collider isTrigger 저장
- 포커스 시 FocusAnchor 중앙에 배치하되 월드 크기를 유지 (크기를 1로 강제하지 않음)
- `MaterialPropertyBlock`으로 색상 피드백, 공유 Material 변경 금지
- `RestoreDisplayState()`로 저장 상태와 피드백을 정확히 복원
- 비활성화 시 피드백 제거 (계층 변경은 비활성화 중 불가하므로 Transform 복원은 Focus Controller가 담당)
- 데이터베이스, `ScriptableObject`, 전역 관리자 없음
