# Labeling Editor Commercial Gap and Roadmap

Date: 2026-07-27
Status: Complete
Scope: V7/CVAT 영상 근거와 현재 Labeling Studio 이미지 라벨링 편집기

## 1. 작성 이유

기존 상용 영상 비교는 Dataset Health 시각 QA를 첫 개발 항목으로
선정했고, 현재의 box/polygon/brush/eraser 기반을 CVAT/Supervisely의
기본 조작과 상당 부분 동등한 것으로 표현했습니다.

고밀도 재검토 결과 이 판단은 라벨링 편집기 격차를 과소평가했습니다.
현재 프로그램은 저장·검토가 안전한 기초 편집기를 갖췄지만, V7/CVAT가
제공하는 고처리량 명령 체계, instance-mask 구조 편집, 객체 상태,
대화형 AI correction에서는 명백히 뒤처집니다.

이 문서는 다음 개발자가 “기초 도구가 있으므로 라벨링은 완료”라고
오해하지 않도록 라벨링만 독립적으로 평가하고 구현 계약을 고정합니다.

## 2. 증거 범위

상세 검토 영상:

- `01_V7_Annotations_Getting_Started.mp4`
- `02_V7_Auto_Annotate.mp4`
- `04_CVAT_Bounding_Box_Overview.mp4`
- `05_CVAT_Brush_Mask_Overview.mp4`
- `06_CVAT_AI_Tools_Overview.mp4`

고밀도 접촉 시트:

`artifacts\commercial-video-review-20260727\labeling-detail`

현재 구현 대조:

- 실제 EXE 튜토리얼 object-detection/segmentation 화면
- `WpfAnnotationToolCapabilityService`
- `WpfLearningWorkflowPanelViewModel`
- `WpfLabelingShellWindow.Annotation*`
- `AnnotationSegmentEdit`
- `WpfMaskAnnotationService`
- `WpfObjectReviewEditService`
- `RoiInteractionKeyDown`
- `WpfLabelingShellWindow.ShellInputCommands`
- `WpfLabelingShellWindow.SmartMask`
- `WpfMobileSamBoxPromptService`
- `openvisionlab_mobile_sam_box_prompt.py`

V7/CVAT 02·04·05·06에는 추출 가능한 subtitle stream이 없었습니다.
따라서 이 문서는 화면에서 확인한 상호작용과 상태 변화만 근거로
사용합니다. 음성의 세부 설명, 제품 전체 사양, 마케팅 accuracy는
검증했다고 주장하지 않습니다.

## 3. 상용 편집기의 실제 우위

### 3.1 V7: 한 문맥 안의 annotation correction

관찰된 흐름:

- tool rail에서 brush를 선택하고 shortcut, brush size, eraser 전환을
  즉시 확인
- annotation/instance 목록에서 현재 객체와 class를 관리
- visibility/display 설정을 편집 화면에서 조정
- Segment Anything에서 box 또는 point prompt를 주고 결과를 clear,
  rerun, save
- 불규칙 객체와 다중 instance를 연속 처리하고 사람이 결과를 확정

우리의 차이:

- P0-A 이후 tool/class/repeat/duplicate/help 명령 기반은 존재
- P0-B 이후 MobileSAM 시작 박스, positive/negative point, point
  undo/clear, rerun-replace, 경계 상세도, confirm/skip, 다음 instance
  session은 존재
- 여전히 V7의 광범위한 instance 상태, 구조적 mask 편집, task/video
  propagation과 협업 흐름은 제공하지 않음
- 후보 confirm/skip과 no-autosave는 안전성 강점이므로 유지 대상

### 3.2 CVAT box: 고처리량 생성과 객체 상태

관찰된 흐름:

- 2점/4점 box 생성
- `N`으로 같은 drawing procedure와 parameter 반복
- copy/paste, move, resize, delete
- object list에서 각 객체를 선택하고 상태 관리
- hide/lock/occluded/pin 계열 control과 make copy/propagate

우리의 차이:

- rectangle 생성, move/resize, ROI copy/paste는 존재
- copy/paste는 canvas 하위 계약에 있으나 라벨링 명령 체계와 shortcut
  help가 일관되지 않음
- 일반 selected-object duplicate, repeat last shape+class, 4점 생성이 없음
- per-object 상태는 class 변경과 delete 중심이며 hide/lock/occluded/pin
  계약이 없음

### 3.3 CVAT mask: raster paint를 넘어선 구조 편집

관찰된 흐름:

- add/remove brush와 size/shape 조절
- hole과 multi-component instance
- join/merge, slicing/splitting
- mask/polygon 변환과 정밀 point edit
- instance color, z-order, occlusion
- remove-underlying 실행 전 경고와 영향을 받는 객체 처리

우리의 차이:

- `WpfMaskAnnotationService`의 사용자 기능은 Paint, Erase,
  `TryMoveRasterMask` 중심
- 내부 span merge는 paint 성능 구현이지 mask object merge 기능이 아님
- polygon은 전체 이동과 근접 point 이동은 가능하지만 vertex
  insert/delete, split/join 계약은 없음
- raster/polygon/canonical JSON/mask PNG/YOLO/CVAT 사이의
  multi-component·hole·z-order 의미가 아직 제품 계약으로 고정되지 않음

### 3.4 CVAT AI: detector와 interactor의 correction loop

관찰된 흐름:

- detector model, task/current range, label mapping, confidence, 진행 상태
- positive point, negative point, 시작 box
- mask-to-polygon과 polygon detail
- 여러 객체를 연속 생성·보정
- intelligent scissors와 image equalization 계열 표시 보조
- tracker는 영상 영역이므로 현재 제품 범위 밖

우리의 차이:

- batch detection은 표시 행 범위, progress/stop, threshold,
  Candidate Review를 제공
- generic per-run class mapping과 통합 preflight가 없음
- box-only MobileSAM은 안전한 후보 흐름이지만 correction loop는 아님
- point prompt를 “box-jitter 96/96 통과” 때문에 계속 보류할 근거는 없음

## 4. 현재 편집기 기능의 정확한 기준선

### 4.1 지원 도구

capability service에는 Select, Rectangle, PanZoom, Delete, Ellipse,
Polygon, Brush, Eraser, Undo, Redo가 연결된 것으로 선언됩니다.
그러나 실제 purpose별 선택 도구는 다음과 같습니다.

- object detection: Select, Rectangle, PanZoom
- segmentation: Brush, Eraser, Polygon, Select, PanZoom
- anomaly: PanZoom

Ellipse는 내부 capability에 존재하지만 현재 object-detection 또는
segmentation의 주 생산 도구 목록에 포함되지 않으므로 상용 비교에서
지원 주력 기능으로 계산하지 않습니다.

### 4.2 geometry 편집

- rectangle: 생성, 선택, 이동, resize, copy/paste
- polygon: point 생성, 첫 point/double-click 종료, right-click draft
  cancel, 전체 이동, 근접 point 이동
- raster mask: paint, erase, 전체 이동
- history: undo/redo

부족한 항목:

- repeat last shape+class
- 일반 selected-object duplicate
- 4점/extreme-point box
- polygon vertex insert/delete
- edge-aware scissors
- mask merge/split/hole/multi-component/z-order/remove-underlying

### 4.3 객체 상태와 표시

현재:

- object list
- class 변경/apply
- delete
- labels only / inference only / both
- mask opacity
- brush size

부족:

- per-object hide/lock/pin
- occluded/tag/group의 Recipe/export 의미
- z-order
- brightness/contrast/gamma/invert/histogram/equalization

### 4.4 명령과 shortcut

현재 확인된 명령:

- `Ctrl+Z`, `Ctrl+Shift+Z`, `Ctrl+Y`
- queue 이전/다음 image
- canvas copy/paste 하위 기능

부족:

- tool hotkey
- class `1~9` hotkey와 10개 이상 class fallback
- `N` 계열 repeat
- selected-object duplicate의 일관된 key
- shortcut cheat sheet와 text-entry conflict 정책

### 4.5 AI 보조

현재 box-only MobileSAM:

1. 마지막 rectangle을 prompt로 선택
2. worker가 box 하나로 mask 후보 생성
3. prompt rectangle 제거
4. pending candidate 하나 적용
5. 작업자가 confirm 또는 skip
6. confirm 전에는 canonical label로 저장하지 않음

보호할 강점:

- 명시적 후보 검토
- no-autosave
- image/prompt generation guard
- source immutability
- runtime/weight provenance
- 기존 96-call box-jitter regression

추가해야 할 기능:

- positive/negative point
- point undo/clear
- rerun 시 새 후보 누적이 아닌 현재 후보 replace
- output polygon detail
- next-instance session
- stale result 차단과 UI responsiveness

## 5. 라벨링 편집기 성숙도

| 평가 축 | 점수 | 가중치 | 현재 판단 |
| --- | ---: | ---: | --- |
| 기본 shape와 직접 편집 | `3.5/5` | 15% | 저장 가능한 box/polygon/brush/eraser와 선택 폴리곤 정점 추가·삭제 |
| 반복·고처리량 생산성 | `2.8/5` | 15% | P0-A shortcut/repeat/duplicate/help 완료; batch propagation은 제외 |
| mask/geometry 구조 편집 | `3.8/5` | 20% | P1-A/P1-B 계약과 merge/join, axis-aligned split/slice, enclosed hole add/fill, saved-object z-order, remove-underlying 완료 |
| 대화형 AI correction | `3.5/5` | 20% | P0-B box+positive/negative correction, rerun-replace, 상세도, 다음 객체 완료 |
| 객체 상태·metadata | `2.0/5` | 10% | P2 session-only hide/full-lock/movement-pin, compact state icons, contextual segment options 완료; 영속 tag/group/occlusion은 미구현 |
| 이미지·overlay 가시성 | `3.5/5` | 10% | compact display-only brightness/contrast/gamma/invert/equalization과 overlay 좌표 불변 검증 |
| 저장·검토 안정성 | `4.0/5` | 10% | Recipe/no-autosave/canonical save/provenance |
| 가중 합계 | **`3.4/5`** | 100% | contextual Object Review, precision geometry, compact display-only aids를 검증한 구조적 워크플로 추정 |

이 점수는 제품 전체 완성도나 모델 정확도가 아닙니다. 현재 제품 전체의
집중형 로컬 워크플로 평가는 `4.0/5`로 유지하되, 라벨링 편집기 깊이는
영상 재평가 당시 기준선 `2.1/5`에서 P0-A/P0-B 완료 후 `2.5/5`, 첫
P1-C merge/join 완료 후 `2.6/5`, axis-aligned split/slice 완료 후
`2.7/5`, enclosed hole add/fill 완료 후 `2.8/5`, saved-object z-order 완료
후 `2.9/5`, remove-underlying 완료 후 `3.0/5`, P2 session-only
hide/lock/pin 1차 완료 후 `3.1/5`로 관리했으나, 상용 영상을 다시 확인해
pin을 금색 북마크로 해석한 오류와 모든 구조 명령 상시 노출 문제를 발견했습니다.
movement-pin과 맥락 UI의 focused/protected/current-build 1920/1366 검증을
완료한 뒤 polygon vertex insert/delete의 focused/protected/current-build
1920/1366 검증도 통과해 현재는 `3.2/5`로 관리합니다. 남은 P2 edge-aware
precision geometry도 독립 정확도/지연시간 fixture, 명시적 preview/apply/
cancel, protected/canonical 회귀와 current-build 1920/1366 검증을 통과해
`3.3/5`로 관리했습니다. P3 display-only brightness/contrast/gamma/invert/
equalization도 source/file/history/overlay 불변 계약과 current-build
1920/1366 검증을 통과해 현재는 `3.4/5`로 관리합니다. persistent metadata,
video propagation, collaboration이 남아 있으므로 CVAT/V7 parity로
해석하지 않습니다.

## 6. 개발 계약

### 6.0 실행 상태

| 항목 | 상태 | 현재 근거 | 완료 전 남은 증거 |
| --- | --- | --- | --- |
| P0-A command/productivity | **Complete** | 단축키·반복·복제·도움말 구현, focused 회귀, current-source 및 actual-EXE 1920x1080 증거 통과 | 상용 parity는 아니며 이후 회귀 시에만 재개 |
| P0-B interactive Smart Mask | **Complete** | 실제 box+positive+negative worker, session/stale/replace/no-autosave 계약, actual-EXE confirm/next-instance 통과 | field validation은 `Not evaluated`; 이후 회귀 시에만 재개 |
| P1 mask structure | **Complete** | preservation/loss, canonical v3 identity, merge, axis-aligned split, enclosed hole add/fill, explicit saved-object stack order, two-step remove-underlying, undo/redo와 save/load/re-save 통과 | polygon/raster exact visual interleaving은 renderer 후속 |
| P2 object state/precision | **Complete** | session-only hide/full-lock/movement-pin, contextual options, zoom-aware polygon vertex insert/delete, bounded edge-aware intelligent scissors, protected/canonical 회귀와 1920/1366 증거 완료 | 임의 자연영상/field 정확도와 CVAT/V7 parity는 주장하지 않으며 회귀 시에만 재개 |
| P3 display aids | **Complete** | compact `보기 보정`, source/file/history/overlay 불변 test, 1920/1366 current-build 증거 | production-camera usefulness와 CVAT/V7 parity는 주장하지 않음 |
| P4 visual QA | **Complete** | 문제 우선 목록, 선택 이미지 저장-overlay, 문제 필터, 기존 editor 이동, 1920/1366 증거 | 회귀 시에만 재개 |
| P5 interchange/batch preflight | **Complete** | P5-A는 포맷 변환 격리 Dry-run/명시적 Apply, P5-B는 scope/model/weight/task/confidence/Recipe class-name mapping/existing-label policy와 명시적 Start를 제공 | production-camera/cross-session 모델 품질 근거는 별도 외부 선행조건 |

P0-A 개발 시작 기준:

- 시작 HEAD: `ef155ed docs: close structural refactoring phase`
- 변경 전 빌드: `OpenVisionLab.LabelingStudio.csproj` Debug, 경고 0·오류 0
- 변경 전 화면:
  `artifacts\ui\labeling-productivity-p0a-20260727\before-current-source-1920x1080.png`
- 실제 EXE smoke 두 경로는 각각 guide/tools tab 선택과 segmentation 목적
  선택 자동화가 캡처 전에 실패했습니다. 이는 P0-A 코드 변경 전 발견된
  자동화 한계이며, 개발 완료 후 current-build actual-EXE를 다시
  시도하고 current-source after 증거는 반드시 새로 생성합니다.

P0-A 완료 증거:

- `--labeling-productivity`: shortcut mapping, 11-class fallback, 반복 상태,
  box/polygon/raster-mask deep-copy와 안전 offset 통과
- `--wpf-undo-redo-shortcuts`: 실제 shell `R`, `1`, `N`, `F1`, `Ctrl+D`,
  text-entry 차단, tool/class 유지, canonical save, duplicate 1회 undo/redo 통과
- `--wpf-roi-object-verification`,
  `--wpf-segmentation-object-verification`,
  `--segmentation-annotation-storage`, `--wpf-mask-drag-performance`,
  `--roi-drawing-preview-performance` 통과
- 변경 전:
  `artifacts\ui\labeling-productivity-p0a-20260727\before-current-source-1920x1080.png`
- 변경 후 current-source:
  `artifacts\ui\labeling-productivity-p0a-20260727\after-current-source-1920x1080.png`
- 변경 후 최신 실제 EXE:
  `artifacts\ui\labeling-productivity-p0a-20260727\after-actual-exe-1920x1080.png`
- 실제 EXE에서 OpenGL airspace가 도움말 overlay를 가리는 문제를 발견해,
  도움말을 캔버스 위 별도 Auto 행으로 이동하고 전용
  `--exe-labeling-productivity-smoke`로 재검증했습니다.

P0-B 완료 증거:

- `WpfSmartMaskPromptSessionService`가 image/Recipe/box/class/generation,
  positive/negative point, 입력 mode, 48/96/256 경계 상세도와
  next-instance 상태를 소유합니다.
- worker는 단일 시작 box에 반복 `--point x,y,label`과
  `--max-polygon-points`를 받아 기존 box-only 호출과 호환됩니다.
- 실제 합성 fixture에서 positive point는 mask area를 `4431 → 6529`로
  확장했고, positive+negative 결과는 넓은 positive 결과의
  `44512 → 22399`로 오검출 영역 `22113` pixel을 제거했습니다.
- source image SHA-256은 실행 전후 동일했고, runtime은 MobileSAM /
  Ultralytics `8.4.101` / Torch `2.12.1+cpu` / CPU, weight SHA-256은
  `6DBB90523A35330FEDD7F1D3DFC66F995213D81B29A5CA8108DBCDD4E37D6C2F`입니다.
- rerun은 confirmed label을 유지하면서 pending candidate 하나만
  교체하고, image/Recipe/prompt generation mismatch 결과는 적용하지
  않습니다.
- 후보 생성 중 UI는 async 상태를 유지하며 `생성 취소`가 worker
  cancellation token을 취소합니다.
- actual Debug EXE에서 box → 포함점 1 → 제외점 1 → rerun → confirm →
  next instance → 새 box 준비가 통과했습니다.
- 변경 전:
  `artifacts\ui\smart-mask-p0b-20260727\before-current-source-1920x1080.png`
- 변경 후 current-source:
  `artifacts\ui\smart-mask-p0b-20260727\after-current-source-1920x1080.png`
- 실제 EXE 보정 후보:
  `artifacts\ui\smart-mask-p0b-20260727\after-actual-exe-before-confirm-1920x1080.png`
- 실제 EXE 확정 후:
  `artifacts\ui\smart-mask-p0b-20260727\after-actual-exe-after-confirm-1920x1080.png`
- real-worker 수치:
  `artifacts\mobile-sam-point-correction\20260727-185324\point-correction-evidence.json`
- 증거 경계: 합성 fixture 기능 재현이며 생산 카메라 정확도는
  `Not evaluated`입니다.

### P0-A. Labeling Command and Productivity Foundation

#### 목표

도구를 매 객체마다 toolbar에서 다시 찾는 비용을 없애고, box와
segmentation 작업 모두가 사용하는 명령 정책을 만듭니다.

#### 포함

- tool hotkey: Select, Rectangle, Polygon, Brush, Eraser, PanZoom
- class `1~9` hotkey
- 10개 이상 class의 검색/목록 fallback과 충돌 없는 정책
- repeat last shape+class
- selected box/polygon/raster-mask duplicate
- 완료 후 현재 tool/class 유지 정책
- 화면에서 열 수 있는 shortcut cheat sheet
- text box/combobox/editor focus에서는 drawing shortcut을 차단하는
  deterministic key routing

#### 제외

- Viewer 입력 엔진 재작성
- 새 포맷 또는 새 AI model
- video frame propagate
- cloud/team shortcut profile

#### 소유권

Current owner:

- tool capability: `WpfAnnotationToolCapabilityService`
- purpose별 tool 선택: `WpfLearningWorkflowPanelViewModel`
- shell key bridge: `WpfLabelingShellWindow.ShellInputCommands`
- geometry copy/move/resize: `OpenVisionLab.ImageCanvas\RoiInteraction`
- annotation history: 기존 WPF Annotation history service

Intended owner:

- command enablement, repeat state, class/tool persistence, visible shortcut
  text: WPF `Annotation` ViewModel/Service
- shell: key/event bridge만 담당
- ImageCanvas: geometry hit-test, move/resize, 저수준 duplicate primitive만
  담당
- save/history: 기존 annotation domain owner 재사용

#### 완료 기준

| 기준 | 증거 |
| --- | --- |
| text entry 중 drawing shortcut이 실행되지 않음 | deterministic key-routing test |
| `1~9`가 해당 class를 선택하고 10개 이상 class fallback이 동작 | ViewModel/service test |
| 같은 shape+class를 toolbar 재선택 없이 반복 생성 | WPF labeling focused test |
| duplicate가 class/geometry를 보존하고 안전한 offset 또는 명시적 same-position 정책을 사용 | box/polygon/mask fixture |
| duplicate/repeat 각각 한 번의 undo로 되돌아감 | history regression |
| save/reopen 후 canonical geometry와 class가 동일 | storage regression |
| 기존 ROI/brush 성능 경로가 유지됨 | 관련 protected focused gates |
| 실제 EXE에서 shortcut help와 반복 흐름을 확인 | 1920x1080 before/after와 smoke |

Recommended model: `gpt-5.6-terra`
Reasoning effort: `medium`

### P0-B. Interactive Smart Mask Refinement

#### 목표

box-only 후보 생성을 V7/CVAT처럼 사람이 수정 가능한 명시적 correction
session으로 확장하되, 자동 저장하지 않습니다.

#### 포함

- 시작 box
- positive point
- negative point
- point undo/clear
- rerun
- rerun 시 pending candidate replace
- polygon detail/point-density 선택
- current instance confirm/skip
- confirm 후 next instance 시작
- prompt/candidate overlay의 구분

#### 제외

- text prompt
- video tracking
- task-wide automatic segmentation
- 자동 confirm/save
- 새 weight 자동 download
- field accuracy 주장

#### 소유권

Current owner:

- UI session: `WpfLabelingShellWindow.SmartMask`
- process contract: `WpfMobileSamBoxPromptService`
- worker: `openvisionlab_mobile_sam_box_prompt.py`
- candidate confirm: 기존 Candidate Review/confirmation service

Intended owner:

- prompt/candidate generation state: WPF Annotation-assist ViewModel/Service
- shell: canvas point와 command bridge만 담당
- worker request/result: Model/Annotation assist contract
- confirm/save: 기존 canonical segment owner 유지

#### 상태 안전 규칙

- Recipe 또는 image가 바뀌면 이전 generation 결과를 무시합니다.
- rerun은 후보를 쌓지 않고 현재 pending candidate를 교체합니다.
- candidate는 confirm 전 JSON/PNG/YOLO label에 기록하지 않습니다.
- source image와 external dataset은 변경하지 않습니다.
- worker/model/weight/runtime provenance를 기록합니다.
- 기존 box-only 96-call matrix는 regression으로 유지합니다.

#### 완료 기준

| 기준 | 증거 |
| --- | --- |
| positive point가 fixture의 누락 영역을 후보에 추가 | real-worker focused fixture |
| negative point가 fixture의 오검출 영역을 후보에서 제거 | real-worker focused fixture |
| clear/rerun이 prompt state와 후보를 결정적으로 갱신 | service state test |
| rerun이 pending candidate를 중복 생성하지 않고 replace | WPF/candidate regression |
| image/Recipe switch 후 stale result가 적용되지 않음 | generation/cancellation test |
| confirm 전 canonical save가 없음 | storage assertion |
| confirm 후 기존 segment JSON/mask PNG 경로로 저장 | canonical save regression |
| worker 중 UI가 응답하고 cancel/skip 가능 | WPF responsiveness smoke |
| 실제 EXE에서 box+positive+negative+confirm 흐름 확인 | 1920x1080 before/after와 actual-EXE smoke |

Recommended model: `gpt-5.6-sol`
Reasoning effort: `high`

### P1. Mask Structure and Occlusion Editor

Status update (2026-07-27):

- P1-A preservation/loss contract is complete.
- `SegmentationInterchangeContractService` is the code authority for canonical
  JSON, mask PNG, YOLO, COCO, and CVAT capability/warning semantics.
- COCO/CVAT/YOLO export results now expose deduplicated warnings for detected
  conditional or lost semantics; a cutout fixture reports `Holes: Lost`.
- This does not complete P1 editor parity and does not raise the current
  labeling-editor score.
- P1-B canonical schema v3 is complete. Disconnected components retain one
  object ID, component indices, z-order, and last structural operation across
  save/load/re-save. Version 1/2 geometry remains backward compatible.
- P1-C merge/join is complete as the first user-visible structural command.
  Same-class polygon/raster sources become one raster object with a new v3
  identity, `Merge` provenance, one-step undo/redo, and save/load/re-save
  evidence.
- P1-C axis-aligned split/slice is complete. One selected polygon/raster source
  is cut by a one-pixel row/column only when 2+ components result, with new
  identities, `Split` provenance, one-step undo/redo, and v3 re-save evidence.
- Hole editing, saved-object z-order, and remove-underlying are complete.
  Remove-underlying uses a read-only impact analysis, orange affected-object
  preview, explicit confirmation/cancel, stale-plan rejection, one-step
  history, and canonical v3 replay. Exact polygon/raster overlay interleaving
  remains a renderer-level limitation and is not claimed.
- Evidence:
  `docs\SEGMENTATION_INTERCHANGE_PRESERVATION_CONTRACT_20260727.md` and
  `docs\SEGMENTATION_MERGE_P1C_20260727.md`, and
  `docs\SEGMENTATION_SPLIT_P1C_20260727.md`; focused gates are
  `--segmentation-interchange-contract`, `--segmentation-merge`, and
  `--segmentation-split`.

#### 선행 계약

먼저 아래 표현의 round-trip 의미를 fixture로 고정합니다.

- canonical polygon/segment JSON
- raster mask PNG
- YOLO segmentation
- COCO segmentation
- CVAT mask/polygon
- hole, multi-component, z-order, remove-underlying의 보존 또는 명시적
  손실 경고

#### 기능

- merge/join
- split/slice
- hole/multi-component
- z-order
- remove-underlying와 영향 객체 경고
- 각 작업의 undo/redo

Recommended model: `gpt-5.6-sol`
Reasoning effort: `high`

### P2. Object State and Precision Geometry

- hide/full-lock/movement-pin은 label data가 아닌 presentation/session state
- hidden은 canvas/structural edit에서 제외하고 row 재선택은 유지
- locked는 class/delete/geometry/brush/eraser/structural direct mutation까지 차단
- movement pin은 whole-object 이동만 차단하고 ROI resize, polygon vertex edit,
  copy/delete/class/structural command는 허용함
- gold review focus와 `PINNED` overlay label은 잘못된 해석이므로 사용하지 않음
- 상태 아이콘은 compact selected-object property로 유지하고, merge/split/hole/
  z-order/remove-underlying은 manual segment 선택 때만 접힌 options로 노출
- image/queue 전환에서 reset하고 history/dirty state에는 포함하지 않음
- occluded/tag/group은 Recipe/export 소비 계약이 있을 때만 영속화
- 4점/extreme-point box
- polygon vertex insert/delete — **Complete; zoom-aware hit, invalid-result rejection, one-step history, v3 replay**
- edge-aware intelligent scissors — **Complete; deterministic 90%/2.5px·250ms fixture, explicit preview/apply/cancel, one-step history, v3 replay**

Recommended model: `gpt-5.6-sol`
Reasoning effort: `high`

### P3. Display-Only Image and Overlay Aids

- **Complete**
- compact canvas-header `보기 보정` popup
- brightness, contrast, gamma, invert, histogram equalization
- 120ms slider input coalescing and queue-navigation setting retention
- canonical source/file hash, dirty/history, overlay image-coordinate invariants
- current-build 1920x1080 and 1366x768 visual evidence
- evidence: `docs\DISPLAY_ONLY_IMAGE_AIDS_P3_20260728.md`

Recommended model: `gpt-5.6-terra`
Reasoning effort: `medium`

### P4. Dataset Health Visual Label QA

기존 read-only 시각 검토 계약은 폐기하지 않고 순서만 뒤로 이동합니다.
gallery는 편집기가 아니라 데이터 전체에서 문제 항목을 찾고 기존
라벨링 화면으로 이동하는 분석 surface입니다.

Status update (2026-07-28): `Complete`.

- detection/segmentation/anomaly 저장 상태를 이미지별로 분류하고 문제를
  먼저 표시합니다.
- worklist는 text metadata만 가지며 선택한 한 장만 최대 800px로
  지연 디코딩합니다.
- 저장된 box, polygon, raster boundary를 읽기 전용으로 합성합니다.
- `문제만` 필터와 `편집기에서 열기`를 제공하되 Dataset Health 안에서
  수정·승인·저장하지 않습니다.
- 누락/손상 fixture, source SHA-256 불변, 기존 editor route,
  current-build 1920/1366 근거가 통과했습니다.
- 상세 계약과 증거:
  `docs\DATASET_HEALTH_VISUAL_QA_P4_20260728.md`.

Recommended model: `gpt-5.6-terra`
Reasoning effort: `medium`

### P5. Interchange and Batch Preflight

- 기존 COCO/Pascal VOC/Label Studio/CVAT service dry-run/Apply
- image/label pairing, malformed record, class/split, source immutability
- batch AI scope, model/weight, class mapping, confidence, existing-label policy
- explicit Start와 Candidate Review/no-autosave 유지

Recommended model: `gpt-5.6-terra`
Reasoning effort: `medium`

## 7. 보호할 현재 강점

상용 기능을 추가하면서 다음을 약화시키지 않습니다.

- Recipe source-of-truth
- canonical save/reopen
- undo/redo
- Candidate Review와 no-autosave
- explicit confirm/skip
- local model provenance
- Image Queue/10K Worklist
- Dataset Health와 dataset-version/model evidence
- Preview/Run/학습의 명시적 실행
- output layer 생성이 input layer를 자동 변경하지 않는 규칙
- Viewer/OpenGL/ROI/brush/eraser 성능 경로

## 8. 범위 밖

- video interpolation/tracking
- comment, assignment, multi-reviewer history
- account, cloud sync, deployment
- 3D, keypoint
- arbitrary model marketplace
- camera/lighting/PLC/I/O control
- AI 후보의 자동 승인·저장

## 9. 개발 순서

1. P0-A command/productivity foundation — **Complete**
2. P0-B interactive Smart Mask — **Complete**
3. P1 mask structure contract와 editor — **Complete; merge/join·split/slice·hole·saved-object z-order·remove-underlying 완료**
4. P2 object state/precision — **Complete; contextual UI, movement-pin, polygon vertex insert/delete, bounded edge-aware scissors 완료**
5. P3 display-only aids — **Complete**
6. P4 visual QA — **Complete**
7. P5 interchange/batch preflight — **Complete; P5-A format conversion and P5-B batch AI**

한 단계가 acceptance criteria와 현재 EXE 증거를 통과하기 전에 다음
단계를 자동으로 시작하지 않습니다.

## 10. Completion Record

Status: Complete

Scope: V7/CVAT의 실제 이미지 라벨링 흐름을 현재 source/UI와
항목별로 대조하고, 라벨링 편집기 성숙도와 P0-A~P5 구현 계약을
문서화했습니다.

Acceptance criteria:

- 상용 관찰과 현재 구현 사실이 분리됨: pass
- box, mask, AI, object state, shortcut 격차가 각각 기록됨: pass
- 라벨링 편집기 영상 기준선 `2.1/5`, P0-A/P0-B 완료 후 `2.5/5`, 첫
  P1-C merge/join 완료 후 `2.6/5`, split/slice 완료 후 `2.7/5`
  및 hole add/fill 완료 후 `2.8/5`, saved-object z-order 완료 후 `2.9/5`,
  remove-underlying 완료 후 `3.0/5`, P2 session-only hide/lock/pin 1차 완료 후
  `3.1/5`였고, 상용 영상 기반 의미/UI 정정 동안 `3.0/5`로 환원한 뒤
  protected 회귀와 1920/1366 증거 통과 후 `3.1/5`로 복원하고 polygon
  vertex insert/delete 완료 후 `3.2/5`, bounded edge-aware scissors 완료 후
  `3.3/5`, P3 display-only aids 완료 후 `3.4/5`로 갱신: pass
- P0-A/P0-B의 owner, included/excluded scope, state safety, 완료 기준이
  구현 가능한 수준으로 정의됨: pass
- protected behavior와 out-of-scope가 명시됨: pass
- P4 문제 우선 visual QA, 선택 이미지 지연 overlay, 기존 editor 이동과
  source 불변이 focused/visual evidence로 완료됨: pass

Verification:

- V7/CVAT 고밀도 접촉 시트 시각 검토
- current source, tests, actual-EXE tutorial 화면 대조
- subtitle stream 부재 확인
- 문서 변경 후 isolated build, `--priority-workflow-docs`,
  `git diff --check` 실행

Evidence:

- `artifacts\commercial-video-review-20260727\labeling-detail`
- `docs\LABELING_STUDIO_COMMERCIAL_VIDEO_REVIEW_20260727.md`
- 이 문서

Boundary / next dependency: P0-A/P0-B와 P1-C merge/join·split/slice·hole·saved-object z-order·remove-underlying,
P2 session-only hide/full-lock/movement-pin의 상용 영상 기반 의미/UI
정정, polygon vertex insert/delete, bounded edge-aware intelligent scissors와
P3 display-only aids, P4 Dataset Health visual QA와 P5-A format-conversion
preflight는 protected/focused regression과 1920/1366 current-build 증거를
통과했습니다. P5-B batch AI preflight도 scope/model/weight/task/confidence,
Recipe class-name mapping, 기존 라벨 정책, 명시적 Start,
Candidate Review/no-autosave 계약과 1920/1366 증거를 통과했습니다.
다음 제품 선행조건은 독립 production-camera/cross-session 데이터입니다.
saved-object z-order는 canonical 전역 순서와
polygon/raster 각 overlay family 내부 표시 순서를 보장하지만 두
family의 정확한 교차 합성 순서까지 보장하지 않습니다. 현재
완료는 CVAT/V7 parity, field accuracy, video propagation, cloud/team
platform을 의미하지 않습니다.
