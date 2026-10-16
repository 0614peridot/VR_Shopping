# Project Structure

## Project Name

ShoppingVR

## Summary

Unity 기반의 3D 가상 옷가게에서 사용자가 1인칭 VR 시점으로 매장을 둘러보고, 의류 및 패션 아이템을 선택해 여러 각도로 돌려보며 확인하고, 원하는 상품을 장바구니에 담을 수 있는 가상 쇼핑 프로토타입이다.

## Key Concept

ShoppingVR의 핵심은 매장에서 관심 상품을 간단히 선택하고, 한눈에 살펴본 뒤 장바구니에 담는 직관적인 VR 쇼핑 경험이다.

- 사용자가 상품을 클릭하거나 선택하면 해당 상품 하나만 시야 중앙에 포커스된다.
- 포커스된 상품은 오브젝트 조작이 활성화되어 여러 방향으로 회전하며 확인할 수 있고, 동시에 선택 상품을 담기 위한 장바구니 UI가 나타난다.
- 장바구니 아이콘 버튼을 선택하면 현재 포커스된 상품이 장바구니에 담긴다.
- 화면 오른쪽 위에는 작은 장바구니 아이콘이 항상 표시되며, 배지 숫자로 현재 담긴 상품 수를 보여준다.
- 다른 상품을 선택하거나 선택을 해제하면 기존 포커스와 조작 상태가 정리되어 한 번에 하나의 상품만 활성화된다.

## Key Mechanism

핵심 동작은 일반 탐색 상태인 `Explore`와 상품을 살펴보는 `Focused`의 두 상태로 구성한다. 현재 XR 환경이 구성되지 않은 프로토타입 단계에서는 키보드와 마우스로 전체 흐름을 확인한다.

### 매장 탐색과 상품 선택

- 사용자는 `W/A/S/D` 또는 방향키로 매장 안을 이동한다.
- 마우스로 상품을 클릭하면 해당 상품 하나가 선택되고 `Focused` 상태로 전환된다.
- 화면 오른쪽 상단에는 사용자 시야를 따라오는 tag-along 장바구니 아이콘을 표시하고, 배지 숫자로 현재 담긴 상품 수를 보여준다.

### 포커스 모드와 상품 회전

- 선택한 상품을 사용자 시야의 전면 중앙에 표시하고 오브젝트 조작을 활성화한다.
- 화면 상단에는 상품 이름과 가격, 하단에는 `장바구니에 담기` 버튼, 왼쪽 상단에는 `나가기` 버튼을 표시한다.
- 사용자가 상품을 마우스로 누른 채 드래그하면 드래그 방향에 따라 상품이 회전하고, 마우스 버튼을 놓으면 회전이 멈춘다.
- 포커스 중에는 이동 입력을 잠가 이동과 상품 회전이 충돌하지 않도록 하며, UI 버튼 위의 마우스 입력은 상품 회전을 시작하지 않는다.

### 장바구니와 포커스 종료

- `장바구니에 담기` 버튼을 클릭하면 현재 상품을 장바구니에 추가하고 오른쪽 상단 배지 숫자를 즉시 갱신한다. 상품은 계속 살펴볼 수 있도록 포커스 모드를 유지한다.
- `Esc` 키 또는 왼쪽 상단 `나가기` 버튼으로 동일한 종료 동작을 실행한다.
- 포커스 종료 시 회전 입력, 선택 상태, 포커스 UI와 시각 피드백을 정리하고 상품의 진열 상태를 복원한 뒤 이동 입력을 다시 활성화한다.
- 다른 상품을 선택하거나 현재 상품이 비활성화되는 경우에도 기존 포커스를 먼저 정리해 한 번에 하나의 상품만 활성화한다.

## Tech Spec

### 기술 기준과 범위

- Unity `6000.6.4f1`, URP `17.6.0`, Input System `1.20.0`, uGUI `2.6.0`을 기준으로 한다.
- 현재 XR Interaction Toolkit, XR Plug-in Management, OpenXR은 설치되어 있지 않으므로 키보드와 마우스로 핵심 쇼핑 흐름을 검증한다. 이 명세만을 위해 XR 패키지나 대규모 Project Settings 변경을 추가하지 않는다.
- 레거시 `UnityEngine.Input`을 사용하지 않고 기존 `Assets/InputSystem_Actions.inputactions`의 `Player/Move`, `UI/Point`, `UI/Click`, `UI/Cancel`을 `InputActionReference`로 연결한다. 기존 액션으로 구현할 수 없을 때만 최소한의 액션을 추가하고 변경 사항을 보고한다.
- 새 스크립트는 `Assets/ShoppingVR/Scripts/` 아래에 배치하며, 모든 새 타입과 파일명에 `CS_` 접두사를 적용한다.
- 결제, 재고, 저장, 장바구니 상세 목록, 상품 삭제와 수량 편집은 구현하지 않는다.

### 생성할 클래스 구조

| 클래스 및 파일 | 부모/종류 | 역할 |
| --- | --- | --- |
| `CS_ShoppingMode.cs` | `enum` | `Explore`, `Focused` 두 상호작용 상태를 정의한다. |
| `CS_PlayerLocomotion.cs` | `MonoBehaviour` | `CharacterController`와 Move 액션을 사용해 `W/A/S/D` 및 방향키 이동을 처리한다. 포커스 중에는 `SetMovementEnabled(false)`로 이동을 잠근다. |
| `CS_ProductItem.cs` | `MonoBehaviour` | 상품명, 원 단위 가격, 선택용 Collider와 피드백용 Renderer를 보유한다. 포커스 전 진열 상태를 저장하고 종료 시 부모, Transform, 물리 상태와 시각 피드백을 복원한다. |
| `CS_ProductFocusController.cs` | `MonoBehaviour` | 마우스 포인터 Raycast, 단일 상품 선택, `Explore ↔ Focused` 전환, 포커스 위치 배치, 드래그 회전, 종료 정리를 총괄한다. |
| `CS_FocusUIController.cs` | `MonoBehaviour` | 포커스 패널의 상품명, 가격, `장바구니에 담기`, `나가기` 버튼 표시와 버튼 이벤트 연결을 담당한다. |
| `CS_CartCounter.cs` | `MonoBehaviour` | 런타임 장바구니 개수와 우측 상단 배지 표시를 관리한다. 담기 요청 한 번당 개수를 1 증가시키며 저장이나 상세 목록은 관리하지 않는다. |

#### `CS_PlayerLocomotion`

- `[RequireComponent(typeof(CharacterController))]`를 사용한다.
- 이동 속도는 미터/초 단위의 `[SerializeField] private` 값으로 두고, 카메라 기준 수평 방향과 `Time.deltaTime`을 사용해 `CharacterController.Move`를 호출한다.
- 입력 액션은 `OnEnable`에서 활성화하고 `OnDisable`에서 비활성화한다.
- 포커스 시작 전 이동 활성 상태를 보존하고 종료 시 이전 상태를 복구한다.

#### `CS_ProductItem`

- 상품명, 가격, Collider, Renderer 참조는 Inspector에서 설정한다. 현재 범위에서는 별도 상품 데이터베이스나 `ScriptableObject`를 만들지 않는다.
- 포커스 진입 전에 원래 부모, 로컬 위치·회전·크기와 필요한 `Rigidbody`/Collider 값을 저장한다.
- 포커스 중에는 단순 색상, Emission 또는 작은 크기 변화 중 한 방식으로 선택 상태를 표시한다. 공유 Material을 직접 변경하지 않고 `MaterialPropertyBlock`처럼 다른 상품에 영향을 주지 않는 방법을 사용한다.
- 포커스 종료 시 저장한 상태를 정확히 복원한다.

#### `CS_ProductFocusController`

- 직렬화 참조로 사용자 Camera, 카메라 앞의 `FocusAnchor`, 상품 LayerMask, 최대 선택 거리와 필요한 협력 컴포넌트를 받는다.
- `Explore`에서 Click 액션이 수행되면 현재 포인터 위치로 Raycast하고, 가장 가까운 `CS_ProductItem` 하나만 선택한다. EventSystem이 UI 위 포인터를 감지하면 상품 선택을 실행하지 않는다.
- 포커스 진입 순서는 `기존 상품 정리 → 현재 상품 설정 → FocusAnchor 배치 및 피드백 → 포커스 UI 표시 → 이동 잠금`으로 고정한다.
- `Focused`에서는 상품 위에서 시작한 왼쪽 마우스 드래그의 포인터 이동량에 회전 감도(도/픽셀)를 곱해 yaw와 pitch를 갱신한다. 마우스 버튼 release/cancel 시 즉시 회전을 멈추며, UI 위에서는 회전을 시작하지 않는다.
- `ExitFocus()`를 `Esc`, `나가기` 버튼, 대상 변경과 예외 정리에서 함께 사용하는 단일 종료 경로로 만든다.
- 종료 순서는 `드래그 취소 → 상품 상태 복원 → UI 숨김 → 현재 상품 해제 → 이동 복구 → Explore 전환`으로 고정한다.
- 포커스 상품이 비활성화·파괴되거나 컨트롤러가 비활성화되어도 UI와 이동 잠금이 남지 않도록 같은 정리 규칙을 적용한다.

#### `CS_FocusUIController`

- 포커스 패널, 상품명과 가격 텍스트, 담기 버튼, 나가기 버튼을 직렬화 참조로 받는다.
- `Show(CS_ProductItem)`은 텍스트를 갱신하고 패널을 표시하며, `Hide()`는 패널을 숨긴다. 가격은 천 단위 구분과 `원` 접미사를 사용한다.
- 담기 버튼은 현재 포커스 상품이 있을 때만 `CS_ProductFocusController.AddCurrentProductToCart()`를 호출한다. 추가 후 포커스는 유지한다.
- 나가기 버튼은 `ExitFocus()`를 호출한다. 버튼 listener는 활성화·비활성화 시 대칭적으로 등록하고 해제한다.
- UI는 상품 Transform이나 전역 상태를 직접 변경하지 않는다.

#### `CS_CartCounter`

- 초기 개수는 0이며, 동일 상품을 다시 담아도 각 버튼 입력을 한 건으로 계산한다.
- 개수 변경 직후 우측 상단 장바구니 배지를 갱신한다. 0일 때 배지를 숨길지는 Inspector 옵션으로만 제공할 수 있다.
- 싱글턴, 전역 이벤트 버스, 저장소 계층을 만들지 않고 Scene의 직렬화 참조로 연결한다.

### Scene 및 UI 구성

- Player 루트에는 `CharacterController`와 `CS_PlayerLocomotion`을 두고, Camera 앞에는 빈 `FocusAnchor`를 둔다. 상품이 카메라를 가리거나 벽을 뚫지 않도록 거리를 Inspector에서 조정한다.
- 각 Primitive 상품에는 Collider와 `CS_ProductItem`을 추가하고 상품 전용 Layer를 사용한다.
- uGUI Canvas는 현재 데스크톱 검증에서 Screen Space 방식으로 구성한다. 우측 상단 장바구니 아이콘은 시야를 자연스럽게 따라가며 항상 표시하고, 포커스 패널은 시작 시 숨긴다.
- EventSystem에는 `InputSystemUIInputModule`을 사용해 UI 클릭과 상품 선택/회전 입력을 구분한다.
- 실제 HMD용 World Space 또는 tag-along UI와 VR 컨트롤러 입력은 XR 런타임과 대상 기기가 확정된 뒤 별도로 구성하고 검증한다.

### 데이터 및 상태 흐름

1. `Explore`에서 Click 입력을 받으면 `CS_ProductFocusController`가 상품 Raycast를 실행한다.
2. 상품이 선택되면 기존 포커스를 정리하고 새 상품의 진열 상태를 저장한 뒤 `FocusAnchor`에 배치한다.
3. 상품 정보 UI와 선택 피드백을 표시하고 플레이어 이동을 잠근 다음 상태를 `Focused`로 변경한다.
4. 마우스를 누른 채 드래그하는 동안 현재 상품만 회전한다.
5. 담기 버튼을 누르면 `CS_CartCounter`가 개수와 배지를 갱신하고 포커스는 유지한다.
6. `Esc`, 나가기 버튼, 대상 변경 또는 대상 비활성화 시 `ExitFocus()`가 상태와 입력을 정리하고 `Explore`로 복귀한다.

### 검증 원칙

- 먼저 Unity Console의 컴파일 오류와 예외가 0인지, 대상 Scene이 Play Mode에 진입하는지 확인한다.
- Play Mode에서 `이동 → 상품 클릭 → 단일 중앙 포커스 → 상품명·가격 표시 → 이동 잠금 → 드래그 중 회전 → 담기와 배지 증가 → Esc/나가기 → 진열 상태와 이동 복구`의 전체 흐름을 순서대로 확인한다.
- UI 위 클릭이 상품 선택이나 회전을 일으키지 않고, 드래그를 멈추면 회전도 즉시 멈추는지 확인한다.
- 다른 상품 선택, 빠른 반복 입력, 포커스 상품 또는 컨트롤러 비활성화 상황에서도 둘 이상의 활성 상품, 남은 UI, 잠긴 이동, 예외가 없어야 한다.
- 장바구니 버튼 입력 한 번당 배지가 정확히 1 증가하고, 추가 후 포커스가 유지되는지 확인한다.
- 모든 새 타입과 파일의 `CS_` 접두사, 필수 직렬화 참조, 레거시 Input 미사용, 관련 없는 Scene·패키지·설정 보존 여부를 확인한다.
- 매 프레임 `Find`, 반복 `GetComponent`, LINQ 또는 불필요한 할당을 만들지 않는다.
- 단순 카운트와 상태 전이는 필요할 때 `CS_CartCounterTests`, `CS_ProductFocusControllerTests` 같은 Edit Mode 테스트로 검증한다. Raycast, UI, Transform 연동은 Play Mode 테스트나 수동 Play Mode 검증을 사용한다.
- 실행하지 않은 검증은 통과했다고 기록하지 않는다. 헤드셋 없이 확인한 결과는 `Editor verified`, 실제 기기 검증 전 항목은 `device verification required`로 구분한다.

### 응답 원칙

- 사용자 입력에는 즉시 이해할 수 있는 피드백을 제공한다. 상품 선택 시 같은 프레임에 포커스 UI와 시각 피드백을 표시하고, 담기 성공 시 배지 숫자를 즉시 갱신한다.
- 무효한 입력은 상태를 손상시키지 않으며, 필수 참조가 없으면 반복 로그 대신 한 번의 명확한 오류를 남기고 해당 동작을 안전하게 중단한다.
- 작업 완료 보고에는 변경한 파일과 역할, 구현한 사용자 흐름, Scene/Inspector 연결 방법, 실제 수행한 검증, 남은 제한사항을 간결히 기록한다.
- 패키지, Project Settings, Input Actions 또는 Scene 전체 설정을 변경했다면 변경 이유와 정확한 항목을 별도로 알린다.
- `Editor verified`, `device verified`, `device verification required`를 실제 검증 범위에 맞게 사용한다. 실행하지 않은 테스트나 헤드셋 동작을 성공으로 표현하지 않는다.
- 요청 범위 밖의 결제, 재고, 저장, 상세 장바구니 또는 XR 기능을 구현했거나 완료했다고 보고하지 않는다.
