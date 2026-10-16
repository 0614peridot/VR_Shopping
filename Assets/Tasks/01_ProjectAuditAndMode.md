# Task 01 — Project Audit and Mode

## 참고 문서

- `MDs/00_ProjectStructure.md`
- `MDs/Workflow.md` (섹션 "1. 프로젝트 점검과 상태 정의")

## 선행 Task

없음

## 구현 범위 (이 파일만 생성)

- `Assets/ShoppingVR/Scripts/CS_ShoppingMode.cs`

그 외의 C# 파일, 패키지, Project Settings, Input Actions, Scene은 변경하지 않는다. 문제가 있으면 수정하지 않고 보고한다.

## 점검 항목

1. 버전 확인
   - `ProjectSettings/ProjectVersion.txt`: Unity `6000.6.4f1`
   - `Packages/manifest.json` / `packages-lock.json`: URP `17.6.0`, Input System `1.20.0`, uGUI `2.6.0`
2. Active Input Handling
   - `ProjectSettings/ProjectSettings.asset`의 `activeInputHandler` 값 확인 (`1` = Input System Package (New), `2` = Both, `0` = Old)
3. `Assets/InputSystem_Actions.inputactions` 액션/바인딩 확인
   - `Player/Move`: `W/A/S/D`, 방향키
   - `UI/Point`: 마우스 위치
   - `UI/Click`: 마우스 왼쪽 버튼
   - `UI/Cancel`: `Esc` 포함
4. XR 패키지 (XR Interaction Toolkit, XR Plug-in Management, OpenXR) 설치 여부 — 없으면 설치하지 않고 제한사항으로 기록
5. 레거시 `UnityEngine.Input` 사용 여부 확인 (프로젝트 스크립트 대상)

## 구현

`CS_ShoppingMode`는 `Explore`, `Focused` 두 값만 갖는 `enum`이다.

```csharp
public enum CS_ShoppingMode
{
    Explore,
    Focused
}
```

- 네임스페이스, 추가 값, 유틸리티 메서드를 넣지 않는다.
- `.meta` 파일은 Unity가 생성하도록 둔다 (직접 만들지 않는다).

## 검증

- 가능하면 Unity 에디터 컴파일(또는 batchmode)로 오류 0 확인. 실행하지 못했다면 체크하지 않고 이유를 기록한다.
- `Assets/Checklists/WorkflowChecklist.md`의 Task 01 항목만 실제 확인한 것만 갱신한다.

## 보고

변경 파일, 점검 결과(항목별 실제 값), 수행한 검증, 남은 제한사항.
