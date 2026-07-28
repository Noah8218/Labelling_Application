# Labeling Studio 상용 프로그램 연동 영상 비교 및 개발 계약

Date: 2026-07-27
Status: Complete
Source: `C:\Git\GoPxL_Video\새 폴더`

Latest implementation synchronization: 2026-07-28

- P0-A through P5-B are complete and verified as bounded local-workstation
  slices.
- Smart Mask is now auto-first and contextual; a real six-sample correction
  evaluator records both improvement and failure, and initial/latest candidate
  restore protects the operator from a worse rerun.
- Current scores are labeling-editor depth `3.4/5` and focused local
  workstation `4.0/5`.
- Actual Debug EXE restore/save/reopen replay is complete under
  `artifacts\operator-video\20260728-smartmask-restore-save-retry1`.
  Documentation source-of-truth synchronization is the next maintenance item.
  Four-point box, persistent object metadata, and polygon/raster cross-family
  z-order remain contract-dependent backlog; independent model adoption
  remains data-blocked.

## 1. 목적과 정정 사항

이 문서는 제공된 상용 프로그램 연동 영상 10개를 현재
OpenVisionLab Labeling Studio와 비교하고, 제품 방향에 맞는 개발
우선순위를 고정합니다.

초기 검토는 Dataset Health 시각 QA를 가장 큰 격차로 보았으나,
V7과 CVAT의 라벨링 영상을 더 촘촘한 간격으로 다시 검토한 결과 이
판단을 정정합니다.

- 현재 프로그램은 box, polygon, brush, eraser, 저장, undo/redo라는
  안정적인 기초를 보유합니다.
- 그러나 V7과 CVAT는 반복 생성, 단축키, 객체 상태, 구조적 mask 편집,
  대화형 AI 보정이 하나의 편집 흐름으로 결합되어 있습니다.
- 따라서 현재 프로그램의 기초 도구 보유를 상용 편집기와의 기능
  동등성으로 해석할 수 없습니다.
- 즉시 개발 우선순위는 Dataset Health 시각 QA가 아니라 라벨링
  편집기의 생산성과 대화형 보정 깊이입니다.

상세 라벨링 격차, 점수 근거, 구현 계약은
`docs\LABELING_EDITOR_COMMERCIAL_GAP_AND_ROADMAP_20260727.md`에
기록합니다.

## 2. 제품 방향과 평가 경계

현재 제품 정체성은 다음과 같습니다.

- 로컬 Windows 단일 작업자용 산업 이미지 라벨링·학습·추론·검토
  워크스테이션
- Recipe가 이미지, 클래스, 주석, split, 데이터 버전과 모델 근거의
  source-of-truth를 소유
- Preview, 추론, 학습, 후보 확정, 모델 채택은 명시적 작업자 행동
- AI 결과는 자동 저장하지 않고 Candidate Review 또는 명시적 확인을
  거침
- Viewer/OpenGL/ROI/brush/eraser 성능 경로를 보호

이번 비교에서 개발 목표로 삼지 않는 범위:

- 계정, 작업 배정, 다중 검토자, 댓글, 승인 이력, 클라우드 동기화
- 호스팅·배포, 카메라 제어, PLC/I/O, 산업 제어 플랫폼
- 비디오 추적, 3D, keypoint, 범용 임의 모델 실행
- 상용 제품 UI의 외형 복제

## 3. 평가 점수

| 비교 범위 | 현재 평가 | 의미 |
| --- | ---: | --- |
| 집중형 로컬 단일 작업자 전체 워크플로 | `4.0/5` | 약 80%; 설정·저장·학습·추론·검토·모델 근거를 포함한 제품 전체의 범위 적합도 |
| 이미지 라벨링 편집기 깊이 | `2.1/5` | 약 42%; V7/CVAT 영상에서 확인한 이미지 편집 생산성·mask 구조·AI 보정과 비교한 별도 추정치 |
| 일반 상용 이미지 라벨링 제품군 전체 | `3.4/5` | 약 68%; contextual Object Review, precision geometry, compact display-only aids 완료 후에도 persistent metadata, video propagation, 협업 범위는 뒤처짐 |
| 엔터프라이즈/팀 플랫폼 | `1.2/5` | 약 24%; 의도적으로 목표로 삼지 않는 범위 |

`2.1/5`는 정확도 benchmark가 아니라 영상에서 확인한 기능과 현재
source/UI를 항목별로 대조한 구조적 워크플로 추정치입니다. 전체 제품
점수 `4.0/5`와 모순되지 않습니다. 전체 제품은 데이터 설정, 학습,
추론, 검토, 모델 근거까지 포함하지만 `2.1/5`는 라벨링 편집기만
평가합니다.

현재 상태 주의: `2.1/5`는 P0-A/P0-B 구현 전 영상 재검토 기준선이고
`2.5/5`는 두 slice 직후의 역사적 점수입니다. P1~P5와 정밀 편집,
문맥형 Smart Mask 후속까지 반영한 현재 라벨링 편집기 평가는 `3.4/5`입니다.
계산과 완료 증거는
`docs\LABELING_EDITOR_COMMERCIAL_GAP_AND_ROADMAP_20260727.md`가 source of
truth입니다.

## 4. 검토 방법과 증거 한계

1. `ffprobe`로 파일, 길이, 해상도와 스트림 구성을 확인했습니다.
2. 10개 영상 각각의 전 구간을 덮는 16시점 1920x1080 접촉 시트를
   만들었습니다.
3. V7/CVAT 라벨링 영상 5개는 6~15초 간격의 고밀도 접촉 시트를 추가로
   만들고 실제 도구·객체 상태·보정 흐름을 다시 확인했습니다.
4. 현재 source, tests, 2026-07-22 실제 EXE 튜토리얼 화면을 대조했습니다.

일반 접촉 시트:

`artifacts\commercial-video-review-20260727`

V7/CVAT 고밀도 접촉 시트:

`artifacts\commercial-video-review-20260727\labeling-detail`

중요한 한계:

- V7/CVAT 02·04·05·06 영상에서 추출 가능한 subtitle stream을 찾지
  못했습니다.
- 따라서 자막 또는 음성 전사를 검토했다는 주장은 하지 않습니다.
- 관찰 결과는 화면에 보이는 조작, 상태 변화, 데모 문구에 한정합니다.
- 영상의 마케팅성 속도·정확도 표시는 독립 benchmark로 사용하지
  않습니다.
- 제공 영상은 각 제품 전체 사양을 증명하지 않습니다.

## 5. 영상별 관찰과 현재 판단

| 번호 | 파일 | 길이 | 영상에서 확인한 핵심 흐름 | 현재 판단 |
| --- | --- | ---: | --- | --- |
| 01 | `01_V7_Annotations_Getting_Started.mp4` | 05:45 | 도구 rail, brush 단축키/크기/eraser 전환, annotation·instance 목록, 표시 설정, review 영역, SAM box/point 보정 | 이미지 편집 흐름이 우리보다 깊음. video timeline·댓글은 제외하지만 tool/state/AI correction 통합은 핵심 참고 |
| 02 | `02_V7_Auto_Annotate.mp4` | 03:22 | 거친 box로 불규칙 객체 mask 생성, 다양한 소재·군집·결함 사례, 빠른 반복 보정과 사람 검토 | 현재 box-only MobileSAM과 동등하지 않음. 대화형 보정과 반복 작업 흐름이 명백한 제품 격차 |
| 03 | `03_Label_Studio_Getting_Started.mp4` | 07:56 | 프로젝트 생성, 데이터 가져오기, 작업 지시, 다음 미완료 작업, XML 기반 UI 구성 | 데이터·작업 맥락과 다음 미완료 이동 참고. 범용 XML 제품화는 제외 |
| 04 | `04_CVAT_Bounding_Box_Overview.mp4` | 03:14 | 2점/4점 box 생성, `N` 반복, 복사/붙여넣기, 이동·resize, 객체 목록, lock/hide/occluded/pin 계열 상태, propagate | 단순 발견성 차이가 아님. 반복 라벨링 생산성과 객체 제어가 우리보다 크게 앞섬 |
| 05 | `05_CVAT_Brush_Mask_Overview.mp4` | 12:03 | add/remove brush, hole·multi-component, join/merge, slice/split, mask/polygon 변환, point edit, z-order, 아래 영역 제거와 경고 | 우리 brush/eraser는 기초 raster 편집만 제공. 상용 수준 instance-mask 구조 편집은 주요 격차 |
| 06 | `06_CVAT_AI_Tools_Overview.mp4` | 16:31 | detector 범위·모델·label mapping·confidence·진행, positive/negative point interactor, box 시작, polygon detail, 다중 객체, intelligent scissors, 표시 필터 | batch preflight뿐 아니라 AI correction loop 자체가 주요 격차. tracking만 범위 밖 |
| 07 | `07_Supervisely_How_To_Annotate.mp4` | 02:57 | 클래스/도구 단축키, 같은 클래스 반복, hover 선택, 표시 옵션 | 반복 생산성과 단축키 발견성 근거 |
| 08 | `08_Encord_Image_Annotation.mp4` | 03:01 | box, 객체 속성, frame 분류, 밝기/회전, 작업 제출·검토 | display-only 보정은 유효. 범용 속성·팀 검토는 Recipe 계약이 있을 때만 |
| 09 | `09_LandingLens_Object_Detection_Labeling.mp4` | 02:34 | Pascal VOC 이미지/XML pair 미리보기, 누락·오류 경고, split 지정, 업로드 전 확인 | 구현된 포맷 service를 노출하는 dry-run/Apply 흐름에 유효 |
| 10 | `10_MVTec_DLT_Instance_Segmentation_Labeling.mp4` | 03:42 | 이미지 gallery, 밝기/대비, mask/polygon 편집, smart label, 분할·결합, class/outlier 검토 | 산업 로컬 제품에 가까운 참고. 라벨 편집 우선순위 뒤에 시각 QA로 반영 |

### 5.1 V7에서 배워야 할 점

V7의 우위는 자동 마스크 모델 하나가 아니라 편집기의 연결성입니다.

- tool 선택, class 선택, instance 목록, visibility와 AI refinement가 같은
  문맥 안에 있습니다.
- brush tooltip에 키와 크기 변경, eraser 전환이 함께 노출됩니다.
- Segment Anything 흐름은 box 또는 point 입력 후 clear, rerun, save로
  이어지는 대화형 correction loop입니다.
- 다양한 불규칙 객체를 연속 처리하는 데모는 “후보 하나 생성”보다
  “작업자가 빠르게 고쳐 다음 instance로 이동”하는 제품 흐름을
  강조합니다.

V7 영상에 표시된 수동 polygon 대비 Auto-Annotate 시간 문구는 데모
표현으로만 기록하며, 우리 runtime과의 성능 benchmark로 사용하지
않습니다.

### 5.2 CVAT에서 배워야 할 점

CVAT의 우위는 도구 수보다 고빈도 편집 조작이 서로 연결된 점입니다.

- box: 2점/4점 생성, 같은 설정 반복, 복제, 이동·resize, 객체별
  hide/lock/occluded/pin 계열 상태와 propagate
- mask: add/remove, hole, multi-component, merge/join, split/slice,
  polygon 변환, vertex edit, z-order, remove-underlying
- AI: detector preflight와 진행, class mapping, positive/negative point
  correction, box 시작점, polygon 세부도, 여러 instance 반복
- display: equalization 계열 보기와 edge-aware intelligent scissors

이 기능들은 우리 제품에 모두 그대로 필요한 것은 아니지만, 이미지
라벨링 편집기의 현재 격차가 “단축키 힌트가 조금 부족한 정도”는 아님을
보여 줍니다.

## 6. 2026-07-27 구현 전 검증 기준선

아래 표는 상용 영상 재검토 당시 P0-A~P5 및 Smart Mask 후속 구현 전
기준선입니다. 현재 상태로 사용하지 않습니다. 최신 완료 상태와 남은
항목은 9.1 상태표 및
`LABELING_EDITOR_COMMERCIAL_GAP_AND_ROADMAP_20260727.md`를 따릅니다.

| 영역 | 현재 근거 | 판단 |
| --- | --- | --- |
| 기본 도형 | box, polygon, brush, eraser, select, move, delete, undo/redo | 안정적인 최소 기준선. 상용 편집기 parity는 아님 |
| box 편집 | 이동·resize, ROI 복사/붙여넣기, class 보존 | 유효한 기반. 4점 생성, repeat, 일반 객체 duplicate/state가 부족 |
| polygon/mask | polygon 생성·전체 이동·근접 point 이동, raster paint/erase/전체 이동 | 저장 가능한 기초 편집. point 삽입/삭제와 구조적 mask 연산이 부족 |
| 객체 검토 | 목록, class 변경, delete, image-level quality state | 재작업 기반. 객체별 hide/lock/occluded/tag/z-order가 없음 |
| 단축키 | undo/redo, queue 이동, canvas copy/paste 일부 | tool/class/repeat 명령 체계가 없음 |
| AI 보조 | MobileSAM 단일 box prompt, 후보 confirm/skip, 자동 저장 금지 | 안전한 첫 slice. positive/negative point와 rerun session이 없음 |
| batch AI | 표시 행 대상 detection, progress/stop, threshold, Candidate Review | 후보 검토 원칙은 강점. 범위·label mapping·기존 라벨 정책 preflight가 부족 |
| 표시 보조 | labels/inference/both, mask opacity, brush size | brightness/contrast/gamma/invert/histogram/equalization이 없음 |
| 저장·근거 | Recipe source-of-truth, save/reopen, canonical JSON/PNG/YOLO, dataset/model provenance | 상용 영상만으로 동등성을 확인하지 못한 우리 제품의 강점. 보호 대상 |

현재 `MobileSAM`의 96/96 box-jitter 통과는 고정 합성 데이터에서 box
prompt 허용 오차가 유효함을 증명합니다. 이는 point correction이
불필요하다는 증거도, V7/CVAT 상호작용과 동등하다는 증거도 아닙니다.

## 7. 2026-07-27 구현 전 라벨링 편집기 세부 평가

| 평가 축 | 점수 | 가중치 | 근거 |
| --- | ---: | ---: | --- |
| 기본 shape와 직접 편집 | `3.0/5` | 15% | box/polygon/brush/eraser와 저장·undo/redo 보유 |
| 반복·고처리량 생산성 | `1.8/5` | 15% | tool/class hotkey, repeat, 범용 duplicate와 command help 부족 |
| mask/geometry 구조 편집 | `1.5/5` | 20% | merge/split/hole/multi-component/z-order 계약 부족 |
| 대화형 AI correction | `2.0/5` | 20% | box-only 후보는 있으나 positive/negative point와 rerun session 없음 |
| 객체 상태·metadata | `1.0/5` | 10% | class/delete 중심이며 hide/lock/occluded/tag/pin 부족 |
| 이미지·overlay 가시성 | `1.5/5` | 10% | mask opacity 외 display-only 보정 도구 부족 |
| 저장·검토 안정성 | `4.0/5` | 10% | Recipe, canonical save, Candidate Review, no-autosave 강점 |
| 가중 합계 | **`2.1/5`** | 100% | 반올림한 구조적 워크플로 추정 |

## 8. 개발 우선순위

### P0-A. 라벨링 명령·반복 생산성 기반

tool hotkey, class `1~9` hotkey와 10개 이상 fallback, 마지막
shape+class 반복, 선택 객체 duplicate, 도구·class 유지, shortcut help를
하나의 명령 정책으로 구현합니다.

Recommended model: `gpt-5.6-terra`
Reasoning effort: `medium`

### P0-B. 대화형 Smart Mask refinement

현재 box-only MobileSAM 계약을 보존하면서 positive/negative point,
clear/rerun, 현재 후보 replace, polygon detail, 다음 instance 흐름을
추가합니다. confirm/skip과 no-autosave는 유지합니다.

Recommended model: `gpt-5.6-sol`
Reasoning effort: `high`

### P1. mask 구조·가림 편집

canonical raster/polygon/JSON/PNG/YOLO/CVAT 표현 규칙을 먼저 고정한 뒤
merge/join, split/slice, multi-component/hole, z-order,
remove-underlying+warning+undo를 구현합니다.

Recommended model: `gpt-5.6-sol`
Reasoning effort: `high`

### P2. 객체 상태와 정밀 geometry

per-object hide/lock/pin은 presentation state로 시작합니다.
occluded/tag는 Recipe/export 소비 계약이 있을 때 추가합니다. 4점 box,
polygon vertex insert/delete, edge-aware 도구를 acceptance-gated slice로
분리합니다.

Recommended model: `gpt-5.6-sol`
Reasoning effort: `high`

### P3. 이미지·overlay 표시 보조

brightness, contrast, gamma, invert, histogram/equalization을 source pixel을
바꾸지 않는 display-only 상태로 추가합니다.

Status: **Complete**. 캔버스 헤더의 단일 `보기 보정` 팝업에만 노출했고,
annotation rail에는 추가하지 않았습니다. canonical source/file hash,
dirty/history, overlay image-coordinate 불변과 current-build 1920/1366
증거를 통과했습니다. 상세 계약:
`docs\DISPLAY_ONLY_IMAGE_AIDS_P3_20260728.md`.

Recommended model: `gpt-5.6-terra`
Reasoning effort: `medium`

### P4. Dataset Health 시각적 라벨 QA

기존에 P0로 정했던 read-only overlay gallery는 유효하지만 핵심 라벨링
편집기 slice 뒤로 이동합니다. 기존 Recipe/readiness/quality 상태를
재사용하고 gallery 안에서는 편집하지 않습니다.

Status update (2026-07-28): `Complete`. 문제 우선 text worklist, 선택
이미지 저장-overlay 지연 preview, `문제만`, 기존 editor 이동과 source
불변이 focused/1920/1366 증거를 통과했습니다. 상세 계약:
`docs\DATASET_HEALTH_VISUAL_QA_P4_20260728.md`.

Recommended model: `gpt-5.6-terra`
Reasoning effort: `medium`

### P5. 포맷·batch 실행 preflight

P5-A 포맷 변환은 완료되었습니다. 구현된 COCO/Pascal VOC/Label
Studio/CVAT service를 그대로 재사용하며, 격리 Dry-run, 원본/요청 대상
해시, 건너뜀 차단, 손실 경고, 입력 변경 시 stale 검사 무효화, 명시적
Apply를 별도 창에서 제공합니다.

P5-B는 완료되었습니다. batch detector의 범위·model/weight·task·Recipe
class-name mapping·threshold·기존 label 정책을 Start 전에 검사하고,
실행 결과를 Candidate Review로 보내되 자동 승인·자동 저장하지 않습니다.
근거는 `docs\BATCH_AI_PREFLIGHT_P5B_20260728.md`입니다.

Recommended model: `gpt-5.6-terra`
Reasoning effort: `medium`

### 데이터가 필요한 모델 품질 우선순위

독립 생산 카메라/session 데이터가 없는 detection/anomaly 품질 평가는
기능 개발과 별개의 차단 상태입니다.

Prerequisite: provenance가 확인된 새 데이터와 신뢰 가능한 box/mask 또는
Normal/Abnormal 정답
Recommended model: none until data is available
Reasoning effort: n/a

## 9. 실행 순서와 경계

1. P0-A를 먼저 완료해 편집 명령과 반복 생산성의 공통 기반을 만듭니다.
2. P0-B는 기존 box-only matrix를 regression으로 유지하면서 별도
   acceptance gate로 구현합니다.
3. P1은 저장·상호변환 의미를 먼저 고정하지 않으면 시작하지 않습니다.
4. P2와 P3는 각각 객체 상태와 display-only 상태를 Recipe label
   source-of-truth와 분리합니다.
5. P4, P5-A format-conversion preflight, P5-B batch AI preflight는
   완료되었습니다.
6. Smart Mask auto-first contextual correction, real correction-effectiveness,
   previous/current candidate restore, and actual Debug EXE real-candidate
   restore/save/reopen are complete.
7. Four-point box, persistent object metadata, and exact polygon/raster
   cross-family z-order remain contract-dependent backlog, not permission for
   speculative implementation.
8. Detection/anomaly model adoption requires independent production-camera/
   cross-session data.

다음 항목은 계속 범위 밖입니다.

- video tracking/interpolation 제품화
- comment, assignment, reviewer history, account, cloud collaboration
- 3D, keypoint, arbitrary model marketplace
- source image 자동 변경, AI 후보 자동 저장/자동 승인
- camera/PLC/I/O/deployment 플랫폼

### 9.1 영상 우선순위 최신 상태표

| 영상에서 도출한 항목 | 현재 상태 | 다음 행동 |
| --- | --- | --- |
| tool/class 단축키, 반복, duplicate, help | Complete | 회귀 때만 재개 |
| Smart Mask box/point/rerun/next instance | Complete | 회귀 때만 재개 |
| Smart Mask 자동 우선·문맥형 보정 | Complete | 기본 접힘 유지 |
| 실제 correction 방향성과 실패 보존 | Complete | 정확도 과장 금지 |
| 이전/현재 후보 비교·복원 | Complete | 실제 EXE 저장 replay가 다음 |
| mask merge/split/hole/multi-component | Complete | canonical v3 보호 |
| saved-object z-order/remove-underlying | Complete | cross-family renderer는 별도 |
| hide/full-lock/movement-pin | Complete, session-only | persistent 의미로 확대 금지 |
| polygon vertex insert/delete | Complete | 회귀 때만 재개 |
| intelligent scissors | Complete, bounded | 자연영상 정확도 주장 금지 |
| brightness/contrast/gamma/invert/equalization | Complete, display-only | source 불변 보호 |
| Dataset Health visual QA | Complete | read-only 유지 |
| interchange dry-run/Apply | Complete | 새 포맷 추가보다 기존 계약 보호 |
| batch AI preflight | Complete | Candidate Review/no-autosave 보호 |
| 4-point box | Contract-dependent backlog | geometry/export 계약부터 정의 |
| occluded/tag/group persistence | Blocked by consumer contract | Recipe/export/training/review 소비자 지정 |
| polygon/raster exact cross-family z-order | Conditional backlog | 재현 defect와 renderer 성능 gate 필요 |
| production detection/anomaly adoption | Blocked by data | 독립 camera/session 정답 데이터 확보 |
| video propagation/tracking, team/cloud | Out of scope | 개발하지 않음 |

## 10. 구현 공통 검증 계약

라벨링 UI를 구현할 때마다 다음을 지킵니다.

- 변경 전 현재 EXE 1920x1080 화면을 먼저 캡처
- 변경 후 현재 source로 build한 EXE에서 같은 흐름 캡처
- text input과 shortcut 충돌, tool/class 전환, selection 유지 검증
- save/reopen, undo/redo, image switch, Recipe switch 회귀 검증
- 후보는 confirm 전 canonical label로 저장되지 않음
- source image와 외부 데이터는 변경되지 않음
- Viewer/OpenGL/ROI/brush/eraser 성능 gate 유지
- `--wpf-labeling-shell`, 관련 focused test, `--priority-workflow-docs`,
  `git diff --check` 통과

세부 P0-A/P0-B acceptance criteria는
`docs\LABELING_EDITOR_COMMERCIAL_GAP_AND_ROADMAP_20260727.md`를
따릅니다.

## 11. Completion Record

Status: Complete

Scope: 제공 영상 10개와 V7/CVAT 라벨링 영상 5개의 고밀도 화면 증거를
현재 구현과 비교하고, 라벨링 편집기 격차를 최우선으로 바로잡은 개발
순서와 경계를 문서화했습니다. 제품 코드는 변경하지 않았습니다.

Acceptance criteria:

- V7/CVAT 라벨링 기능이 도구명 나열이 아니라 실제 작업 흐름으로
  기록됨: pass
- 현재 기초 기능과 상용 편집기 parity가 구분됨: pass
- 라벨링 편집기 별도 점수와 근거가 기록됨: pass
- P0-A/P0-B와 후속 P1~P5의 소유권·범위·검증 방향이 정의됨: pass
- team/cloud/video/camera/PLC 제외 범위가 유지됨: pass

Verification:

- 전체 영상 접촉 시트와 V7/CVAT 고밀도 접촉 시트 시각 검토
- 현재 source/test와 actual-EXE 튜토리얼 화면 대조
- subtitle stream 부재 확인; 자막 검토 주장을 제거
- 문서 변경 후 isolated build, `--priority-workflow-docs`,
  `git diff --check` 실행

Evidence:

- `artifacts\commercial-video-review-20260727`
- `artifacts\commercial-video-review-20260727\labeling-detail`
- `docs\LABELING_EDITOR_COMMERCIAL_GAP_AND_ROADMAP_20260727.md`
- 이 문서

Boundary / next dependency: 이 완료 상태는 분석과 개발 계약 문서에만
해당합니다. 라벨링 기능 구현은 각 우선순위의 focused tests와 현재 EXE
before/after 증거가 통과할 때 별도로 Complete가 됩니다.

## 2026-07-28 Object Review 노출 방식 정정

상용 영상은 모든 라벨링 기능을 항상 펼쳐 놓는 구조가 아닙니다. V7은
compact tool rail과 instance list를 주 작업면으로 유지하고, CVAT는 공통
객체 상태를 object row/property 근처에 두며 mask plus/minus와 구조 도구를
현재 tool context에서 노출합니다.

우리 프로그램의 첫 Object Review 구현은 1366x768에서 비활성 merge, split,
hole, z-order, remove-underlying 명령이 전체 높이를 차지해 object list의
보이는 행이 0개가 되는 문제가 확인되었습니다. 정정 계약은 다음과 같습니다.

- 선택 객체의 숨김, 전체 잠금, 이동 고정은 compact icon으로 노출
- manual segment 선택 때만 하나의 접힌 `세그먼트 편집 옵션`을 노출
- cancel과 진행 안내는 해당 interaction이 pending일 때만 노출
- object/instance list를 주 검토면으로 유지
- CVAT식 pin은 전체 위치 이동만 막고 resize, polygon vertex edit,
  copy/delete/class/structural command는 허용
- gold bookmark와 `PINNED` overlay label은 사용하지 않음

맥락 노출은 자동 구조 변경을 의미하지 않습니다. Preview, Apply, label
save, Candidate Review 확인은 계속 명시적 사용자 동작입니다.

구현·검증 계약:
`docs\OBJECT_REVIEW_CONTEXTUAL_UI_CORRECTION_20260728.md`.

## 2026-07-28 지능형 가위 적용 결과

CVAT AI-tools 영상의 intelligent scissors는 전역 자동 라벨링이 아니라
사용자가 시작점/경계를 정하고 결과를 보정하는 correction 도구로
해석했습니다. 현재 프로그램은 이를 선택된 수동 polygon의 접힌
`세그먼트 편집 옵션` 안에 한정했습니다.

- `경계 추종` 후 기존 polygon edge를 클릭
- bounded image-edge path를 금색 open-path로만 미리보기
- `미리보기 적용` 또는 `취소`를 명시적으로 선택
- Apply 전 geometry/history/save 불변
- Apply 후에도 `라벨 저장`은 별도 동작
- deterministic 90%/2.5px·250ms synthetic contract와 protected/canonical
  회귀 통과

이는 CVAT/V7 전체 AI correction parity나 자연영상 field 정확도를
증명하지 않습니다. 구현·검증 계약:
`docs\INTELLIGENT_SCISSORS_P2_20260728.md`.

## 2026-07-28 표시 전용 보정 적용 결과

V7/CVAT 영상에서 image equalization 계열 기능은 매 라벨마다 펼쳐진
annotation tool이 아니라 필요할 때 여는 display option으로 관찰했습니다.
현재 프로그램도 같은 제품 원칙을 적용했습니다.

- 캔버스 헤더에는 `보기 보정` 진입점 하나만 유지
- 밝기/대비/감마/반전/히스토그램 평활화는 popup 안에만 배치
- 원본, 저장 이미지, 학습 입력, annotation history는 불변
- base texture만 바꾸고 box/polygon/mask image coordinates는 불변
- queue image 전환 중 동일 설정을 유지해 비교 가능
- reset은 화면만 원복하며 Preview/Run/save를 자동 실행하지 않음

이 완료로 라벨링 편집기 추정치는 `3.3/5`에서 `3.4/5`로 조정합니다.
집중형 로컬 단일 작업자 전체 워크플로 평가는 `4.0/5`로 유지합니다.
이는 production-camera usefulness, persistent object metadata, video
propagation, collaboration, 또는 CVAT/V7 parity를 증명하지 않습니다.
