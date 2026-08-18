# Recipe Apply Stability

Date: 2026-08-06 KST
Status: Complete

## Goal

Recipe를 같은 이름으로 다시 적용해도 예외가 나지 않고, 디스크 설정 로드가
WPF UI 스레드를 막지 않으며, 여러 적용 요청이 겹치면 마지막 요청만 전역
Recipe/Data 상태를 커밋하도록 한다.

## Ownership

- `WpfProjectRecipeSessionService`가 Recipe 디렉터리 초기화, 설정 로드,
  적용 요청 직렬화, 최신 요청 판정, `CGlobal.Data`와 Recipe identity의 단일
  커밋을 소유한다.
- `WpfLabelingShellWindow.ProjectConfigPersistence.cs`는 입력 검증, 서비스 호출,
  완료 후 패널 갱신과 사용자 상태 문구만 소유한다.
- 새 데이터셋 생성은 이미 저장된 `CData`를 같은 세션 서비스의
  `ApplyPrepared`로 커밋하며, `CRecipe.Name`을 통해 다시 읽지 않는다.
- 앱 시작 시 마지막 데이터셋 복원은 창이 보이기 전의 기존 동기 계약을
  유지한다. 사용자가 누르는 Recipe 적용과 기존 데이터셋 선택은 비동기다.

## Protected Behavior

1. 같은 Recipe를 100회 연속 재적용해도 null task 대기나 예외가 없다.
2. 디스크 설정 로드는 작업 스레드에서 실행되고 UI 후속 갱신은 await 이후에
   진행된다.
3. 적용 요청은 한 번에 하나만 설정을 읽고, 요청 generation이 최신인 경우에만
   전역 상태를 커밋한다.
4. 더 최신인 준비 완료 데이터셋 커밋은 먼저 시작된 디스크 로드에 덮어써지지
   않는다.
5. 취소된 적용은 Recipe 이름과 `CGlobal.Data` 참조를 바꾸지 않는다.
6. 창 종료는 남은 Recipe 적용 token을 취소한다.
7. Recipe 적용 후 저장·재적용 왕복은 저장된 class/config 상태를 복원한다.
8. 라벨 저장, 후보 확정, 모델 채택, active layer 변경은 이 전환에서 자동으로
   실행하지 않는다.

## Verification

Input source: HEAD `822cb7d` with this bounded worktree change.

- isolated build:
  `scripts/Build-LabelingApplicationTests.ps1 -OutputName recipe-apply-final`
  -> pass, 0 warnings, 0 errors, 49.4 seconds;
- focused Recipe/session/UI contract:
  `LabelingApplication.Tests.dll --wpf-project-config-panel`
  -> pass, 9.0 seconds;
- related focused contracts:
  `--wpf-dataset-setup-ui`, `--wpf-dataset-setup-request`,
  `--wpf-labeling-shell`, and `--project-archive`
  -> pass;
- actual EXE Recipe apply and project-panel smoke:
  `--exe-dataset-version-smoke`
  -> pass, dataset version
  `dsv2-922eb6d67c1db03b3bf779bb7a0df300a18cb97f25a354dd11910f9eea381776`;
- actual EXE monitor evidence:
  leftmost `\\.\DISPLAY2`, bounds `-1920,365,1920,1080`;
- protected default suite:
  267 registered checks -> pass, exit code 0, 310.4 seconds;
- `git diff --check` -> pass before documentation closure.

## UI Evidence

All evidence is logically under the repository `artifacts` junction and is
physically stored on `D:\OpenVisionLab-TestData\Labelling_Application`.

- before: `artifacts/ui/recipe-apply-stability-20260806/before-project-config.png`
- after: `artifacts/ui/recipe-apply-stability-20260806/after-project-config.png`
- monitor records: `before-monitor.json`, `after-monitor.json`

The dynamic Recipe name, config path, manifest path, and hash text differ as
expected. The dark palette, control chrome, button state, panel dimensions,
and layout remain unchanged. No control or visual style was added or restyled,
so hover/focus/disabled/popup and Compact-theme matrices are not changed by
this slice.

## Corrective Test Iterations

- The first compile found one missing `System.IO` import for `IOException`; it
  was corrected before the passing build.
- The first focused run exposed a test-harness-only WPF synchronization-context
  deadlock because the test synchronously waited on an awaited UI-context task.
  The test now starts the service contract on a worker thread. The timed-out
  test PID was identified by its exact command line and stopped.
- One immediate rebuild then encountered DLL locks held by that timed-out test
  process. A new isolated output was used after the process was stopped; the
  final build and all tests passed.

## Boundary And Next Dependency

- This does not redesign `CData.SaveConfig`, dataset manifest/version hashing,
  corrupt-XML recovery UX, read-only installation behavior, or Python worker
  stop semantics.
- A separate local reliability contract may decouple ordinary Recipe setting
  saves from synchronous dataset-content hash refresh for large datasets.
- The current project-level P0-C clean-target gate remains externally blocked
  until the selected GPU-capable clean Windows target is directly accessible.

## Durable Closure

```text
Status: Complete
Scope: Safe same-Recipe reapply, asynchronous user-triggered Recipe load, serialized latest-request commit, prepared dataset commit, and close cancellation.
Acceptance criteria: Same Recipe 100x -> pass; overlapping latest request wins -> pass; prepared latest state is not overwritten -> pass; pre-canceled request leaves global state unchanged -> pass; save/reapply round trip -> pass; actual EXE apply -> pass.
Verification: Zero-warning/error isolated build; focused Recipe, dataset, shell, archive checks; leftmost-monitor actual EXE smoke; protected 267-check default suite exit 0; documentation and diff gates.
Evidence: This document, source/tests named above, and artifacts/ui/recipe-apply-stability-20260806.
Boundary / next dependency: Save/hash separation and corrupt/read-only recovery are not part of this slice; external P0-C still requires the GPU-capable clean target.
```
