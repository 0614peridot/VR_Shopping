# Task 04 — Cart Counter

참고: `MDs/00_ProjectStructure.md` (`CS_CartCounter`), `MDs/Workflow.md` 섹션 4. 선행: Task 01.

## 구현 범위

- `Assets/ShoppingVR/Scripts/CS_CartCounter.cs`만 수정

## 요구사항

- 초기 개수 0, `AddOne()` 호출 한 번당 +1, 같은 프레임에 배지 텍스트 갱신
- 같은 상품을 다시 담아도 각 입력을 한 건으로 계산
- 0일 때 배지 숨김은 Inspector 옵션(`hideBadgeWhenEmpty`)으로만
- 텍스트는 uGUI `Text` (TextMeshPro 리소스 임포트 불필요)
- Count Text가 없으면 한 번만 오류 로그
- 상세 목록, 삭제, 수량 변경, 결제, 저장, 싱글턴, 전역 이벤트 버스 없음
