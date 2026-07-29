# 실제 EXE 사용자 기능 감사 (2026-07-29)

## 결론

이 문서는 테스트 메서드 호출만으로 기능을 합격 처리하지 않고, 최신 Debug EXE를 실제 창으로 실행하여 마우스·키보드 입력, 저장, 화면 전환, 재열기를 수행한 결과를 기록한다.

현재 제품 정체성은 로컬 단일 작업자용 산업 이미지 라벨링 워크벤치이다. 데이터셋 시작, 이미지 큐, 클래스, 객체 탐지·세그멘테이션·이상 분류 라벨링, AI 후보 검토, 학습·모델 화면을 한 프로그램에서 연결한다. 영상 전파, 클라우드 협업, 계정·권한, 배포 관리는 현재 범위가 아니다.

문서 기준 성숙도는 집중형 워크벤치 `4.0/5`, 범용 편집기 `3.4/5`이다. 이번 감사는 핵심 라벨 작성과 저장 안전성이 이 평가에 부합함을 확인했고, 실제-EXE 회귀 도구도 최신 내비게이션·데이터셋 선택·전면 창 소유권·WPF/D3D 캡처 계약을 따르도록 보강했다.

## 감사 기준

- 소스 기준: `0f121ae` 이후 이번 감사에서 발견한 작업 패널 명령 바인딩과 실제-EXE 검증 보완 포함
- EXE: `artifacts/run/Debug/OpenVisionLab.LabelingStudio.exe`
- EXE SHA-256: `0235EB13FD820304474771DE400DB05E3E07A2F9567FE6EC596A0A4EB613F0E1`
- 창 크기: 1920x1080
- 실제 입력: UI Automation 탐색 + 네이티브 마우스 이동/클릭/드래그 + 키보드 입력
- 합격 조건: 화면 표시만이 아니라 상태 변화, 파일 저장, 다른 이미지 이동, 재열기 중 해당 기능에 필요한 증거 확인
- 제외: 실제 현장 데이터에서의 모델 정확도, 장시간 학습 완료, GPU별 처리량, 카메라·PLC·클라우드·협업

## 실제 사용자 조작 결과

| 기능군 | 결과 | 실제 확인 내용 | 증거 |
|---|---|---|---|
| 데이터셋 생성 | Pass | 객체 탐지 목적, COCO128 프리셋, Recipe/저장 경로 입력, 128개 이미지·라벨 생성 | `artifacts/user-audit/20260729/core/01_dataset_wizard_after-binding-fix.png` |
| 단계형 내비게이션 | Pass | 1 데이터셋, 2 라벨링, 3 AI 후보, 4 학습/모델과 10개 하위 경로를 실제 클릭. Invoke fallback 0회 | `artifacts/user-audit/20260729/navigation/top-subnavigation-complete.png` |
| 박스 라벨링 | Pass | 새 박스 드래그, 객체 행 생성, 저장, 다른 이미지 이동, 재열기 | 객체 그룹 시나리오에 포함 |
| ROI 선택·삭제 | Pass | 5개 박스 입력, 행 선택, 삭제 활성화, 삭제 후 화면 반응 | `artifacts/user-audit/20260729/feature-matrix/01_roi_tools.png` |
| 브러시·지우개 | Pass | 브러시 5회, 지우개 3회, 도구 전환, 즉시 휠 반응 확인 | `artifacts/user-audit/20260729/feature-matrix/02_mask_tools_final.png` |
| Smart Mask | Pass with limitation | 박스 프롬프트, 자동 후보, 보정 후보, 이전 후보 복원, 확정, 저장, 재열기. 초기 후보 IoU `0.3950`, precision `0.9857`, recall `0.3973` | `artifacts/user-audit/20260729/smart-mask-rerun/` |
| 객체 메타데이터 화면 | Pass | 영구 가림, Recipe 태그, 그룹 구성·그룹 가림·그룹 태그·필터·초기화 진입 상태 확인 | 객체 그룹 최종 화면 |
| 같은 이미지 객체 그룹 | Pass | 박스 2개 드래그, 그룹 구성 시작, 두 행 선택, 그룹 만들기, 라벨 저장, 다른 이미지 이동, 재열기 후 `그룹 1` 두 행 복원 | `artifacts/user-audit/20260729/object-group/object-group-save-reopen.mp4`, `object-group-save-reopen-video.png` |
| 클래스 인덱스 | Pass | 카탈로그의 YOLO 인덱스와 다음 라벨 인덱스 정합성 | `artifacts/user-audit/20260729/feature-matrix/05_class_index.png` |
| Dataset Version v2 | Pass | Recipe 데이터 버전 문자열과 화면 표시 | `artifacts/user-audit/20260729/feature-matrix/06_dataset_version.png` |
| 학습·모델 센터 진입 | Pass | 4단계 진입, 현황/데이터/학습·비교/실행기 탭, 현재·후보 모델과 적용 판단 표시 | `artifacts/user-audit/20260729/navigation/top-subnavigation-complete.png` |
| Dataset Health·교환 규칙 | Pass (내부 교차 검증) | 목적별 요약, 읽기 전용 분석, 원본 불변, 드라이런 계약 | `--dataset-health`, 기본 전체 회귀 |
| 이상 분류 | Pass (내부 교차 검증) | 이미지 단위 판정 저장, 큐 포커스 유지, 클래스 매핑 | 기본 전체 회귀 |
| AI 후보 검토 | Pass | `Recipe 적용` 후 정식 `데이터셋 선택 → 선택 열기` 경로로 전용 1장 큐를 열고, 중심 박스 생성, `실행기 > 테스트`, 후보 1개, 포커스 클릭, 겹친 객체 행 선택을 실제 EXE에서 확인했다. 수동 실행기 설정의 진단 대기는 약 `9.69초`였다. | `artifacts/user-audit/20260730/feature-matrix/04_candidate_focus_complete.png`, 본 문서 검증 기록 |
| 템플릿 일괄 | Pass | 정식 데이터셋 선택 경로로 전용 3장 Recipe를 열고, `Part` 클래스를 명시적으로 선택해 기준 박스를 생성했다. 배치 실행 후 대상 1개·객체 1개 저장, 기존 라벨 미덮어쓰기, 기준 이미지 자기 라벨 미생성, 완료 상태 표시를 확인했다. 배치 저장 대기는 약 `0.59초`였다. | `artifacts/user-audit/20260730/feature-matrix/07_template_batch_complete.png`, 본 문서 검증 기록 |
| 목적 전환 | Pass | 세그멘테이션 마스크 생성, 객체 탐지 전환 후 세그 라벨 숨김, 이미지 밖 박스 무시, 이미지 안 박스 생성, 세그멘테이션 복귀 후 라벨 표시 복원을 실제 EXE에서 확인했다. | `artifacts/user-audit/20260730/feature-matrix/03_purpose_scope_complete.png`, `--exe-purpose-scope-smoke` 실행 로그 |

## 영상과 사진

### 객체 그룹 저장·재열기

- 영상: `artifacts/user-audit/20260729/object-group/object-group-save-reopen.mp4`
- 길이/크기: 70.67초 / 3,105,619 bytes
- 콘택트 시트: `artifacts/user-audit/20260729/object-group/object-group-contact-sheet.png`
- 최종 사진: `artifacts/user-audit/20260729/object-group/object-group-save-reopen-video.png`
- 판정: 박스 2개와 두 행의 `그룹 1` 배지가 저장 후 재열기에서도 유지된다.

### Smart Mask 자동 후보·보정·복원

- 영상: `artifacts/user-audit/20260729/smart-mask-rerun/source/actual-exe-defect-labeling.mp4`
- 콘택트 시트: `artifacts/user-audit/20260729/smart-mask-rerun/evidence/contact-sheet.png`
- 이벤트: `artifacts/user-audit/20260729/smart-mask-rerun/evidence/events.jsonl`
- 자체 평가: `artifacts/user-audit/20260729/smart-mask-rerun/review/self-evaluation.md`
- 판정: 기능 흐름과 저장·재열기는 통과했지만 첫 후보만으로 정답을 가정하면 안 된다. 자동 우선 + 명시적 보정/확정 계약을 유지한다.
- 현재 감사에서 수정한 작업 패널 명령 바인딩은 Smart Mask 경로를 변경하지 않는다. 수정 후 동일 영상을 엄격히 재녹화하려 한 두 실행은 다른 OpenVisionLab 3D Studio 창의 반복적인 전면 점유로 중단했으며, 환경 간섭 실패 화면은 현재 제품 증거로 사용하지 않는다.

## 발견하고 바로 수정한 결함

### P1 접힌 작업 패널 명령 바인딩 누락

재현:

1. `2 라벨링` 단계에 들어간다.
2. 단계 기본 프리셋에 따라 오른쪽 작업 패널이 레일로 접힌다.
3. `열기`, `저장 라벨`, `현재 작업`, `클래스` 바로가기를 누른다.
4. 일부 명령이 창 생성 후 주입된 Shell ViewModel 명령으로 갱신되지 않아 반응하지 않는다.

원인:

- 단계 버튼 4개는 `ConfigureShellCommands` 이후 명령 바인딩을 갱신했다.
- 오른쪽 패널의 확장/축소 및 전체·접힘 바로가기는 같은 갱신 대상에서 빠져 있었다.

수정:

- `WpfLabelingShellWindow.PanelWiring.cs`에서 확장/축소, 단계별 전체 바로가기, 접힘 레일 바로가기 명령 바인딩을 함께 갱신한다.
- `Program.WpfShellStructure.cs`에서 모든 대상이 실제 주입 명령을 참조하는지 고정한다.

검증:

- `--wpf-labeling-shell` Pass
- `--exe-top-subnavigation-smoke` Pass
- 결과: 4개 단계, 표시 바로가기 10개, 실제 클릭 7개, Invoke fallback 0회
- 데이터셋 생성 후 접힌 패널 열기와 저장 라벨 진입 Pass

## 개선 판단

### P1 실제-EXE 녹화의 전면 창 소유권 확인

같은 데스크톱에서 다른 OpenVisionLab 3D Studio 영상 작업이 창을 전면으로 복원하면서 라벨링 앱 좌표 입력과 스크린 캡처를 가린 사례가 반복됐다. 실패 사진에 두 프로그램이 겹쳐 보여 제품 결함과 환경 간섭을 구분할 수 있었다.

회귀 도구의 `RefreshAutomationRoot`는 전면 HWND의 프로세스 소유권을 확인한다. 다른 OpenVisionLab 작업이 전면이면 강제로 빼앗지 않고 최대 45초 기다리며, 해소되지 않으면 `environment-contended`와 PID·프로세스명·실행 경로를 남기고 중단한다. 일반 창만 전면이면 입력 스레드를 연결해 대상 앱을 복원한다. 네이티브 버튼 fallback과 목적/템플릿 드래그도 입력 직전에 소유권을 다시 검사한다. 실제 실행에서 `OpenVisionLab.ThreeD.Shell.exe`와 `OpenVisionLab_Dev`의 `OpenVisionLab.exe`를 제품 실패와 분리했다.

스크린 캡처는 대상 HWND가 실제 전면일 때 사용자에게 보이는 WPF/D3D 합성 화면을 `CopyFromScreen`으로 우선 저장하고, 대상이 전면이 아닐 때만 `PrintWindow(PW_RENDERFULLCONTENT)`를 사용한다. 과거 `PrintWindow` 우선 후보·템플릿 사진의 회색 합성 누락은 시각 합격 증거에서 제외했고, 2026-07-30 재실행으로 목적 전환·후보 포커스·템플릿 일괄의 전체 합성 화면을 새로 저장했다.

### P1 actual-EXE Recipe 적용 경로 최신화

후보 포커스와 템플릿 일괄 actual-EXE 러너는 과거 숨은 `모델` 탭을 직접 선택했다. 공통 Recipe 경로를 현재 `4 학습/모델 > 데이터`로 전환했고, 후보 실행 경로도 `실행기 > 테스트 > 3 AI 후보`로 갱신했다. 후보 1개 생성·포커스·객체 행 선택과 템플릿 대상 저장/기존 라벨 보존을 실제 EXE에서 모두 통과했다.

### P1 수동 실행기 진단의 불필요한 120초 대기

일반 추론은 모델 적재를 위해 기존 120초 이상 연결 대기를 유지한다. 하지만 AutoStartClient=false이고 진단 테스트가 직접 smoke fallback을 허용하는 경우에는 연결될 자동 실행기가 없다는 설정을 이미 알고 있다. 이 경우에만 구성된 검출 제한시간을 1~30초로 제한해 fallback으로 전환한다. 실제 후보 시나리오는 이전 45초 준비 중 정체에서 약 9.65초 내 후보 1개 생성으로 바뀌었다. 배치·학습·일반 현재 이미지 검사의 연결 대기는 변경하지 않았다.

### P1 팝업 도구 메뉴의 실제 사용자 회귀 접근성

현재 `샘플 불러오기`와 템플릿 보조 작업은 상단 설정/도구 팝업에 있다. WPF 팝업은 메인 창 UI Automation 트리와 분리되므로, 러너가 대상 프로세스 소유의 표시 HWND들을 열거하고 각 Automation root에서 버튼과 Expander를 찾도록 보완했다. 실제 후보 fixture 이미지 로드 화면까지 통과했다.

- 팝업 루트의 독립 HWND/Automation tree 식별 -> 구현
- 메뉴 열기 후 실제 버튼 표시·활성화 확인과 클릭 -> 구현·실제 샘플 로드 확인
- 클릭 성공이 아니라 Recipe/sample 상태 변경으로 합격 -> 현재 후보 러너에 반영
- 실패 시 대상 앱 HWND만 캡처 -> 구현·실제 분리 캡처 확인

### P2 Smart Mask 품질 피드백

현재 첫 자동 후보는 precision이 높고 recall이 낮았다. 자동 후보가 생성됐다는 사실만으로 성공처럼 보이지 않도록 다음을 우선한다.

- 후보 품질이 낮을 수 있다는 보정 안내 유지
- 이전/현재 후보 비교의 선택 상태를 더 강하게 표시
- 확정 전 후보 면적 또는 경계 변화 요약 제공
- 현장 샘플에서 클래스별 기본 박스 여백과 보정 전략 평가

### P2 객체 그룹 UI 밀도

그룹 구성은 저장 라벨 패널 안에서 발견 가능하고 실제 저장·재열기까지 동작했다. 다만 `구성/만들기/취소`, 그룹 제거/해제, 그룹 가림/태그가 한 영역에 모여 처음에는 읽을 항목이 많다.

다음 개선은 기능 추가보다 상태별 단순화가 우선이다.

- 평상시: `그룹 구성` 한 버튼과 현재 그룹만 표시
- 구성 중: 행 선택 체크박스, 선택 수, `만들기/취소`만 표시
- 그룹 선택 후: 제거/해제와 그룹 속성만 표시

### P2 실패한 감사 데이터 정리

후보·템플릿 actual-EXE 러너는 각 실행이 만든 정확한 GUID Recipe/DATA/가짜 실행기 폴더를 성공·실패 공통 `finally`에서 정리한다. 기존 과거 폴더는 사용자·과거 테스트 소유권을 구분할 수 없어 삭제하지 않았다. 감사 전용 루트 분리와 실행 전 Recipe 복원 상태 롤백은 별도 후속이다.

## 검증 기록

- `dotnet build .\OpenVisionLab.LabelingStudio.sln -c Debug /nr:false -m:1 /p:UseSharedCompilation=false` — Pass, warning 0, error 0
- `--wpf-labeling-shell` — Pass
- `--exe-dataset-wizard-smoke --verify-object-group` — Pass
- `--exe-top-subnavigation-smoke` — Pass
- `--exe-roi-tools-smoke` — Pass
- `--exe-mask-tools-smoke` — Pass
- `--exe-canonical-class-index-visual` — Pass
- `--exe-dataset-version-smoke` — Pass
- `--object-metadata-review` — Pass
- `--object-group-review` — Pass
- `--smart-mask-candidate-compare-restore` — Pass
- `--smart-mask-auto-boundary-presentation` — Pass
- `--dataset-health` — Pass
- `--wpf-workspace-layout` — Pass
- `--priority-workflow-docs` — Pass
- 기본 전체 내부 회귀 — Pass `260/260`; 실패 표식 0, stderr 0 bytes
- `--exe-purpose-scope-smoke` — Pass: 세그 라벨 숨김·복원, 이미지 밖 박스 무효, 이미지 안 박스 생성; `outsideCreated=False`, `insideCreated=True`
- `--exe-candidate-focus-smoke` — Pass: Recipe 적용, 데이터셋 선택, 전용 샘플 로드, 중심 박스, 실행기 테스트, 후보 1개, 포커스 클릭, 객체 행 선택; `yoloSmokeMs=9693.9`, `focusClickMs=460.6`
- `--exe-template-batch-autolabel-smoke` — Pass: 데이터셋 선택, 명시적 Part 클래스, 기준 이미지/ROI, 배치 클릭, 대상 1개 저장, 기존/기준 라벨 보존, 완료 상태; `batchWaitMs=594.5`

## 완료 기록

Status: Complete

Scope: 최신 EXE의 주요 사용자 흐름 직접 조작, 영상·사진 증거, 접힌 작업 패널 결함 수정, 객체 그룹 실제 저장·재열기, 후보/템플릿 실제 경로 검증

Acceptance criteria:

- 최신 EXE 빌드 및 해시 기록 -> Pass
- 핵심 라벨링·Smart Mask·객체 그룹 영상과 사진 -> Pass
- 데이터셋 생성, 저장, 이동, 재열기 -> Pass
- 1~4단계와 주요 하위 경로 -> Pass
- 브러시·지우개, 클래스, 버전, 모델 센터 진입 -> Pass
- 모든 보조 actual-EXE 러너를 최신 단계·데이터셋 선택 경로에서 재통과 -> Pass; 목적 전환·후보 포커스·템플릿 일괄 모두 2026-07-30 현재 빌드에서 통과
- 사용자에게 보이는 WPF/D3D 합성 화면 캡처 -> Pass; 세 러너 모두 회색 누락 없는 1920x1080 최종 화면 확인
- 실제 현장 모델 학습·추론 정확도 -> Not run; 현장 데이터, 런타임, GPU가 전제

Verification: 위 검증 기록과 `artifacts/logs/default-suite-20260730.out.log`의 기본 전체 회귀 `260/260` Pass 참조

Evidence: `artifacts/user-audit/20260729`, `artifacts/user-audit/20260730/feature-matrix`

Boundary / next dependency: 이 완료는 현재 로컬 단일 작업자 워크플로의 기능·상태·저장 계약을 증명하며 실제 현장 모델 정확도나 생산 성능을 증명하지 않는다. 해당 검증에는 현장 train/valid/test 데이터, 선택 런타임·가중치, GPU가 필요하다.

## 다음 우선순위

1. 객체 그룹 상태별 패널 단순화와 새 실제-EXE 영상 비교 | Recommended model: gpt-5.6-terra | Reasoning effort: medium
2. 실제 모델 학습·후보 모델 비교·검사 적용 | Prerequisite: 현장 train/valid/test 데이터, 선택 런타임·가중치, GPU | 전제 확보 전 모델 토큰 사용 비권장
