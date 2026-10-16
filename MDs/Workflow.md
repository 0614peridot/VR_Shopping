# ShoppingVR Implementation Workflow

## 목적과 현재 범위

이 문서는 `MDs/00_ProjectStructure.md`의 제품 명세를 Unity Editor에서 실행 가능한 프로토타입으로 구현하는 순서를 정의한다.

- 키보드와 마우스로 이동, 상품 선택, 중앙 포커스, 드래그 회전, 장바구니 담기와 수량 표시를 검증한다.
- 현재 설치되지 않은 XR Interaction Toolkit, XR Plug-in Management, OpenXR은 이 워크플로에서 추가하지 않는다.
- 실제 HMD와 VR 컨트롤러 검증은 후속 범위이며 완료 전까지 `device verification required`로 기록한다.
- 기존 `Assets/Scenes/SampleScene.unity`는 덮어쓰지 않는다.

## 작업 원칙

- 작업 전 `AGENTS.md`, `MDs/00_ProjectStructure.md`, 이 문서와 담당 Task 문서를 읽는다.
- `Assets/Tasks`의 Task는 파일명 번호순과 아래 의존성 순서대로 하나씩 서브에이전트로 수행한다.
- Task를 시작하기 전에 선행 산출물, 체크리스트와 Unity Console 상태를 확인한다.
- 각 Task는 문서에 지정된 파일만 구현한다. 선행 의존성이 없으면 자기 범위 밖의 클래스, 임시 스텁 또는 중복 타입을 대신 만들지 않고 `BLOCKED`로 보고한다.
- 모든 새 C# 타입과 파일명에는 `CS_` 접두사를 적용한다.
- 코드 변경 후 Unity의 컴파일이 끝난 다음 Console 오류를 확인하고, 실제 확인한 항목만 체크한다.
- Scene과 Inspector 연결은 관련 스크립트가 모두 컴파일된 뒤 수행한다.
- Unity Scene과 Prefab은 가능하면 Unity Editor에서 수정하고 `.meta` 파일을 보존한다.

## 0. 폴더와 Task 문서 준비

필요할 때 Unity Project 창에서 아래 구조를 만든다.

```text
Assets/
  ShoppingVR/
    Materials/
    Prefabs/
    Scenes/
    Scripts/
    Tests/
  Tasks/
  Checklists/
```

이 문서 작성 시점에는 `Assets/Tasks`가 없다. 다음 Task 문서를 별도 작업으로 작성한 후에만 구현 단계를 시작한다. 문서가 없으면 존재한다고 가정하거나 Task를 실행한 것으로 처리하지 않는다.

| 순서 | 예정 Task 문서 | 구현 범위 | 선행 Task |
| --- | --- | --- | --- |
| 01 | `01_ProjectAuditAndMode.md` | 프로젝트 점검, `CS_ShoppingMode.cs` | 없음 |
| 02 | `02_PlayerLocomotion.md` | `CS_PlayerLocomotion.cs` | 01 |
| 03 | `03_ProductItem.md` | `CS_ProductItem.cs` | 01 |
| 04 | `04_CartCounter.md` | `CS_CartCounter.cs` | 01 |
| 05 | `05_ProductFocusAndUI.md` | `CS_ProductFocusController.cs`, `CS_FocusUIController.cs` | 01~04 |
| 06 | `06_IntegrationValidation.md` | Scene 통합 검증과 기존 책임 범위 내 결함 수정 | 01~05와 Scene 설정 |

`CS_ProductFocusController`와 `CS_FocusUIController`는 직접 협력하므로 중간 컴파일 오류를 피하기 위해 하나의 Task에서 구현한다.

## Task 실행 공통 절차

각 Task는 다음 절차를 따른다.

1. 선행 Task 파일과 지정된 C# 산출물이 실제로 존재하는지 확인한다.
2. `Assets/Checklists/WorkflowChecklist.md`에서 선행 Task 상태를 확인한다.
3. Unity Console에 선행 단계의 컴파일 오류가 없는지 확인한다.
4. 해당 Task 문서에 지정된 파일만 구현하거나 수정한다.
5. 레거시 `UnityEngine.Input`을 사용하지 않았는지 확인한다.
6. Unity의 스크립트 컴파일이 끝날 때까지 기다린 뒤 오류와 관련 경고를 확인한다.
7. 실제 검증한 항목만 체크리스트에 반영한다.
8. 변경 파일, 구현 내용, 검증 결과와 남은 Unity 수동 연결 항목을 보고한다.

“의존성 있는 클래스를 구현하지 말라”는 선행 의존성을 무시하라는 의미가 아니다. 필요한 선행 타입이 준비됐는지 확인하되, 현재 Task가 범위 밖 의존 클래스를 임의로 생성하지 말라는 의미로 적용한다.

## 1. 프로젝트 점검과 상태 정의

`Assets/Tasks/01_ProjectAuditAndMode.md`를 서브에이전트로 수행한다.

1. Unity 버전 `6000.6.4f1`, URP `17.6.0`, Input System `1.20.0`, uGUI `2.6.0`을 확인한다.
2. Active Input Handling이 새 Input System을 사용하는지 확인한다.
3. `Assets/InputSystem_Actions.inputactions`에서 다음 액션과 키 바인딩을 확인한다.
   - `Player/Move`: `W/A/S/D`, 방향키
   - `UI/Point`: 마우스 위치
   - `UI/Click`: 마우스 왼쪽 버튼
   - `UI/Cancel`: `Esc`를 포함하는 Cancel 바인딩
4. XR 런타임 패키지가 없으면 설치하지 않고 제한사항으로 기록한다.
5. `Assets/ShoppingVR/Scripts/CS_ShoppingMode.cs`에 `Explore`, `Focused` 값만 갖는 enum을 만든다.
6. 컴파일 오류가 0인지 확인한 뒤 Task 01 항목을 갱신한다.

## 2. 플레이어 이동 구현

`Assets/Tasks/02_PlayerLocomotion.md`를 서브에이전트로 수행하며 `CS_PlayerLocomotion.cs`만 구현한다.

1. `[RequireComponent(typeof(CharacterController))]`를 적용한다.
2. Camera의 수평 전방·오른쪽 방향과 `Player/Move`를 이용해 이동한다.
3. 이동 속도를 미터/초 단위 직렬화 필드로 두고 `Time.deltaTime`을 적용한다.
4. `SetMovementEnabled(bool)`로 포커스 중 이동을 잠그고 종료 후 복원할 수 있게 한다.
5. 입력 활성화와 비활성화를 `OnEnable`, `OnDisable`에서 대칭 처리한다.
6. 요청하지 않은 점프, 달리기, 복잡한 중력 시스템은 추가하지 않는다.
7. 컴파일 후 Task 02 항목을 갱신한다.

## 3. 상품 상태와 피드백 구현

`Assets/Tasks/03_ProductItem.md`를 서브에이전트로 수행하며 `CS_ProductItem.cs`만 구현한다.

1. 상품명, 원 단위 가격, Collider와 Renderer를 Inspector에서 설정할 수 있게 한다.
2. 포커스 전 부모, 로컬 Transform과 필요한 Rigidbody/Collider 상태를 저장한다.
3. 포커스 시 `MaterialPropertyBlock` 등 공유 Material을 오염시키지 않는 방식으로 단순 피드백을 표시한다.
4. 포커스 종료 시 저장한 진열 상태와 피드백을 정확히 복원한다.
5. 비활성화나 파괴 상황에서 남은 포커스 상태가 없도록 정리한다.
6. 상품 데이터베이스, `ScriptableObject`, 전역 관리자는 추가하지 않는다.
7. 컴파일 후 Task 03 항목을 갱신한다.

## 4. 장바구니 카운터 구현

`Assets/Tasks/04_CartCounter.md`를 서브에이전트로 수행하며 `CS_CartCounter.cs`만 구현한다.

1. 초기 개수는 0으로 설정한다.
2. 담기 요청 한 번당 개수를 1 증가시키고 같은 프레임에 배지 텍스트를 갱신한다.
3. 동일 상품을 다시 담아도 각 버튼 입력을 한 건으로 계산한다.
4. 개수가 0일 때 배지를 숨길지는 Inspector 옵션으로만 제공할 수 있다.
5. 상세 목록, 삭제, 수량 변경, 결제와 저장은 구현하지 않는다.
6. 싱글턴이나 전역 이벤트 버스를 만들지 않는다.
7. 컴파일 후 Task 04 항목을 갱신한다.

## 5. 상품 포커스와 UI 구현

Task 01~04의 파일과 완료 상태를 확인한 뒤 `Assets/Tasks/05_ProductFocusAndUI.md`를 서브에이전트로 수행한다. 이 Task는 다음 두 파일만 구현한다.

- `CS_ProductFocusController.cs`
- `CS_FocusUIController.cs`

구현 순서:

1. 포인터 위치에서 Product Layer만 Raycast하고 `Explore`에서 한 상품만 선택한다.
2. 상품의 진열 상태를 저장한 뒤 Camera 앞 `FocusAnchor`로 이동한다.
3. 포커스 UI와 선택 피드백을 표시하고 플레이어 이동을 잠근다.
4. 상품 위에서 시작한 마우스 드래그로 yaw와 pitch를 회전한다.
5. 마우스 release/cancel 시 즉시 회전을 멈춘다.
6. EventSystem이 UI 위 포인터를 감지하면 상품 선택이나 회전을 시작하지 않는다.
7. 담기 버튼은 현재 상품의 카운트를 1 증가시키고 포커스를 유지한다.
8. 나가기 버튼과 `UI/Cancel`은 같은 `ExitFocus()`를 호출한다.
9. 종료 순서를 `드래그 취소 → 상품 복원 → UI 숨김 → 현재 상품 해제 → 이동 복구 → Explore`로 유지한다.
10. 상품이나 컨트롤러가 비활성화될 때도 UI와 이동 잠금이 남지 않게 한다.
11. 컴파일 후 Task 05 항목을 갱신한다.

## 6. Unity Scene 수동 설정

모든 스크립트가 컴파일된 뒤 Unity Editor에서 수행한다.

### 6.1 프로토타입 Scene

1. `Assets/Scenes/SampleScene.unity`를 연다.
2. `File > Save As`로 `Assets/ShoppingVR/Scenes/ShoppingVR_Prototype.unity`에 저장한다.
3. 중복 Camera와 Audio Listener가 생기지 않도록 기존 Camera를 재사용하거나 하나만 남긴다.

### 6.2 매장 환경

Primitive로 아래 구조를 만든다.

```text
Environment
  Floor
  Wall_Back
  Wall_Left
  Wall_Right
  DisplayShelf_A
  DisplayShelf_B
```

- Floor와 벽에는 Collider를 유지한다.
- 선반은 이동 경로를 막지 않게 배치한다.
- 외부 에셋이나 복잡한 모델은 추가하지 않는다.

### 6.3 Player

```text
Player
  Main Camera
    FocusAnchor
```

1. `Player`에 `CharacterController`와 `CS_PlayerLocomotion`을 추가한다.
2. 시작값으로 CharacterController Height `1.8`, Center Y `0.9`를 사용할 수 있다.
3. Main Camera를 Player의 자식으로 두고 Local Position Y를 약 `1.6`으로 설정한다.
4. `FocusAnchor`를 Camera의 자식으로 만들고 전방 약 `1.2~1.5m`에 배치한다.
5. Camera 참조와 `Player/Move` 액션을 연결한다.

### 6.4 상품

1. Project Settings의 Tags and Layers에 `Product` Layer를 추가하고 이 변경을 보고한다.
2. `Products` 부모 아래에 `Boots`, `Bag`, `Garment`를 만든다.
3. Cube, Capsule 또는 Cylinder와 단순 Material만 사용한다.
4. 각 상품에 Collider와 `CS_ProductItem`을 추가한다.
5. Product Layer, 상품명, 가격, Collider와 Renderer를 설정한다.
6. 선반이나 진열대 위에 배치한다.

### 6.5 Canvas와 EventSystem

```text
Canvas
  CartHUD
    CartIcon
    CartBadge
      CountText
  FocusPanel
    ExitButton
    ProductNameText
    PriceText
    AddToCartButton
EventSystem
```

1. Screen Space Canvas를 만들고 `CartHUD`를 오른쪽 위에 고정한다.
2. `FocusPanel`은 시작 시 비활성화한다.
3. ExitButton은 왼쪽 위, 상품명과 가격은 위쪽, AddToCartButton은 아래쪽에 배치한다.
4. 아이콘 에셋이 없으면 텍스트나 단순 UI Image를 사용한다.
5. EventSystem에는 `InputSystemUIInputModule`만 사용하고 레거시 `StandaloneInputModule`은 함께 사용하지 않는다.
6. `UI/Point`, `UI/Click`, `UI/Cancel`과 필요한 navigation 액션을 연결한다.

### 6.6 시스템과 Inspector 연결

빈 GameObject `ShoppingSystems`에 다음 컴포넌트를 추가한다.

```text
ShoppingSystems
  CS_ProductFocusController
  CS_FocusUIController
  CS_CartCounter
```

Inspector에서 다음 참조를 연결한다.

- 사용자 Camera와 `FocusAnchor`
- Product LayerMask, 최대 선택 거리와 회전 감도
- `CS_PlayerLocomotion`, `CS_FocusUIController`, `CS_CartCounter`
- FocusPanel, 상품명 Text, 가격 Text, 담기 버튼과 나가기 버튼
- CartBadge와 CountText
- `UI/Point`, `UI/Click`, `UI/Cancel`

Scene 저장 전에 Missing 또는 None인 필수 참조가 없는지 확인한다.

## 7. 통합 검증

Scene 설정 후 `Assets/Tasks/06_IntegrationValidation.md`를 서브에이전트로 수행하고 다음 순서로 검증한다.

1. Console 오류 없이 Play Mode에 진입한다.
2. `W/A/S/D`와 방향키로 이동한다.
3. 상품 클릭 시 정확히 하나만 중앙에 포커스되고 상품명과 가격이 표시된다.
4. 포커스 중 이동이 잠긴다.
5. 상품 위에서 마우스를 누르고 드래그할 때만 회전한다.
6. 버튼을 놓으면 회전이 즉시 멈춘다.
7. UI 클릭은 상품 선택이나 회전을 일으키지 않는다.
8. 담기 버튼 한 번당 배지가 1 증가하고 포커스는 유지된다.
9. 나가기 버튼과 `Esc`가 같은 종료 결과를 만든다.
10. 종료 시 상품 부모, Transform, 물리 상태, 피드백과 플레이어 이동이 복원된다.
11. 다른 상품 선택, 빠른 반복 입력, 상품 또는 컨트롤러 비활성화에도 남은 UI, 이동 잠금, 복수 활성 상품이나 예외가 없다.
12. 모든 새 타입과 파일명이 `CS_` 접두사를 사용한다.
13. 레거시 Input, 반복 `Find`/`GetComponent`, 불필요한 매 프레임 할당이 없는지 확인한다.

Task 06에서 발견한 결함은 기존 클래스의 명시된 책임 범위 안에서만 수정한다. 새 기능이나 범위 밖 시스템은 추가하지 않으며 수정 후 영향을 받은 전체 흐름을 다시 검증한다.

## 체크리스트 갱신 규칙

- `[x]`는 파일 확인, 컴파일, Inspector 연결 또는 실행 검증을 실제로 완료한 경우에만 사용한다.
- 아직 수행하지 않은 항목은 `[ ]`로 유지한다.
- 진행할 수 없으면 체크하지 않고 `BLOCKED: 이유`를 기록한다.
- 코드 작성 완료와 Play Mode 검증 완료를 구분한다.
- 결함 수정으로 기존 결과가 무효화되면 해당 항목을 미완료로 되돌리고 재검증한다.
- 한 Task가 후속 Task의 항목을 미리 체크하지 않는다.
- 헤드셋 없이 확인한 항목은 `Editor verified`로 기록한다.
- HMD에서 실행하지 않았다면 Device Validation을 미완료로 두고 `device verification required`라고 기록한다.

## 완료 조건

- Task 01~06이 의존성 순서대로 수행됐다.
- 모든 새 스크립트가 컴파일되고 Unity Console 오류가 없다.
- `ShoppingVR_Prototype` Scene이 Play Mode에 진입한다.
- 이동, 선택, 단일 포커스, 회전, 담기, 배지 갱신, 나가기와 상태 복원 흐름이 연결된다.
- 취소, 비활성화와 반복 입력이 남은 상태나 예외를 만들지 않는다.
- 관련 없는 에셋, 패키지와 Project Settings가 변경되지 않았다.
- 최종 보고에 변경 파일, Scene 연결, 수행한 검증, 미검증 항목과 제한사항이 포함된다.

실제 헤드셋 검증을 하지 않았다면 최종 상태는 `Editor verified / device verification required`로 보고한다.
