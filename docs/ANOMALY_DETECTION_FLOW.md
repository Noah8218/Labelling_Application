# 이상탐지 라벨링·학습·검사 흐름

Status: Current workflow guide as of 2026-08-01.

> PatchCore review update: AI Candidate Review now provides an explicit
> `히트맵 보기` action. Selection checks metadata only; the action opens an
> owned, themed read-only window. Missing or corrupt files fail closed, and
> close/candidate change releases the preview. It does not save, confirm,
> hide, change a layer/viewer state, or adopt a model. This is not a Main
> Viewer layer. See `PATCHCORE_HEATMAP_REVIEW_VIEW_20260801.md`.

## 1. 먼저 구분해야 하는 두 방식

OpenVisionLab Labeling Studio에는 서로 다른 이상탐지 모델 계약이 있습니다.
두 방식 모두 라벨링은 `image-level normal/abnormal` 판정에서 시작합니다.

| 방식 | 학습 데이터 | 결과 | 적합한 상황 |
| --- | --- | --- | --- |
| YOLOv8/YOLO11 분류 | 정상(OK)과 이상(NG) 예시 모두 | 이미지 전체 OK/NG | 이미 알고 있는 결함 유형을 충분히 수집한 경우 |
| PatchCore one-class | 검토 완료 정상 이미지 | 이미지 OK/NG, 이상 점수, 위치 후보, heatmap | NG 종류가 다양하거나 정상 기준에서 벗어난 위치를 먼저 찾고 싶은 경우 |

두 모델의 점수 의미는 같지 않습니다. YOLO 분류의 class confidence와
PatchCore의 nearest-neighbour anomaly score를 같은 숫자로 직접 비교하면
안 됩니다. 같은 held-out 이미지에서 판정 정확도, 놓침, 과검, 위치 근거,
추론 시간을 나란히 비교해야 합니다.

## 2. 공통 라벨링 흐름

1. `1 데이터셋`에서 목적을 `이상 탐지`로 선택합니다.
2. 이미지 루트와 출력 루트를 지정합니다.
3. `2 라벨링`에서 각 이미지를 `정상(OK)` 또는 `이상(NG)`으로 직접
   판정합니다.
4. 폴더 이름을 사용한 미리보기는 제안일 뿐입니다. 사용자가 명시적으로
   반영하기 전에는 저장 판정이 되지 않습니다.
5. 저장된 이미지 판정은 Recipe의 split 규칙에 따라 train/valid/test로
   고정됩니다.

객체 박스나 마스크는 YOLO 이미지 분류 학습의 필수 입력이 아닙니다.
PatchCore도 위치 라벨을 학습하지 않습니다. PatchCore 위치 후보는 정상
특징과 다른 영역을 추론 시 계산한 검토 보조 결과입니다.

## 3. YOLOv8/YOLO11 2클래스 분류

### 학습 조건

- 정상과 이상 이미지가 각각 한 장 이상 검토 완료되어야 합니다.
- 실제 train split에도 normal과 abnormal이 각각 한 장 이상 있어야 합니다.
- 모델 프로필은 `YOLOv8` 또는 `YOLO11`이어야 합니다.
- 시작 가중치는 각각 `yolov8n-cls.pt`, `yolo11n-cls.pt`입니다.

학습 export는 다음 구조를 사용합니다.

```text
<출력 루트>/classification/
  train/normal/
  train/abnormal/
  val/normal/
  val/abnormal/
  test/normal/
  test/abnormal/
```

### 검사 결과

- 결과는 `normal` 또는 `abnormal` 이미지 전체 판정입니다.
- 현재 이상 위치를 제공하지 않습니다.
- 결과는 이미지 큐의 OK/NG 검토 상태로 연결되지만, 사용자의 저장 경계를
  우회하지 않습니다.

## 4. PatchCore 정상-only 이상탐지

### 모델 프로필 선택

1. `4 학습/모델 > 모델 실행 환경 상세`을 엽니다.
2. 모델 프로필에서 `PatchCore`를 선택합니다.
3. 프로필의 `연결`을 눌러 bundled worker와 선택 Python 환경을 점검합니다.
4. 기본 런타임·학습 결과 경로는 D 드라이브가 있으면
   `D:\OpenVisionLab_Runtime\PatchCore`입니다.
5. 정상/이상 판정 매핑이 비어 있으면 `normal`/`abnormal`이 보이는
   기본값으로 채워집니다. 저장 전 편집하거나 기본값으로 되돌릴 수 있습니다.

### 학습 조건

- 이상탐지 Recipe여야 합니다.
- 검토 완료된 normal train 이미지가 최소 2장 필요합니다.
- normal val 이미지가 있으면 임계값 보정에 사용합니다.
- normal val이 없으면 train-normal 기반 보정으로 낮춰 동작하고 화면/로그에
  경고를 남깁니다.
- 검토 완료 NG 이미지는 평가 자료로 export될 수 있지만 메모리 뱅크 학습에는
  포함되지 않습니다.

PatchCore 학습 export는 다음 구조를 사용합니다.

```text
<출력 루트>/patchcore/
  train/normal/       # 메모리 뱅크 학습
  train/abnormal/     # 학습에서 제외, 평가 근거로만 보존 가능
  val/normal/         # 임계값 보정
  val/abnormal/       # 학습에서 제외
  test/normal/
  test/abnormal/
```

학습 결과 `best.pt`에는 다음을 포함합니다.

- ImageNet WideResNet50-2 특징 추출기 상태;
- bounded coreset 메모리 뱅크;
- 정상 calibration 점수에서 얻은 이상 임계값;
- 학습·보정 정상 이미지 수와 생성 시각.

따라서 학습 완료 checkpoint는 추론 시 외부 backbone 다운로드 없이 사용할 수
있습니다. 최초 학습에서 ImageNet 시작 가중치가 로컬 캐시에 없으면 다운로드가
필요합니다. 이 프로젝트의 테스트·모델 캐시는 D 드라이브를 사용합니다.

### 검사 결과와 사용자 동작

PatchCore 검사는 다음을 반환합니다.

- `normal` 또는 `abnormal` 이미지 판정;
- raw anomaly score와 학습 checkpoint의 threshold;
- 이상인 경우 위치 사각형/contour 후보;
- 원본과 anomaly map을 합성한 heatmap PNG.

입력은 전체 프레임을 모델 크기로 리사이즈하며 중앙 크롭으로 가장자리 영역을
버리지 않습니다. 위치 후보 좌표는 anomaly map을 원본 이미지 크기로 다시
확대하여 계산합니다.

위치 후보와 heatmap은 미확정 검토 결과입니다.

현재 heatmap은 PNG 파일로 저장되고 검토 상세에 경로가 표시됩니다. 전용
viewer heatmap layer는 아직 제공하지 않습니다.

- 자동으로 객체 라벨이나 마스크를 저장하지 않습니다.
- 자동으로 현재 검사 모델을 교체하지 않습니다.
- 후보를 표시하거나 가시성을 바꾸는 동작은 Preview/Run 또는 저장을
  자동 실행하지 않습니다.
- 현장 품질 주장은 독립 생산 데이터 검증 전에는 할 수 없습니다.

## 5. 첫 비교 체크리스트

같은 held-out 이미지 집합으로 다음 표를 채웁니다.

| 확인 항목 | YOLO 분류 | PatchCore |
| --- | --- | --- |
| 정상 정답 수 / 전체 정상 |  |  |
| 이상 정답 수 / 전체 이상 |  |  |
| 놓친 이상 수 |  |  |
| 과검 정상 수 |  |  |
| 위치 후보가 실제 결함을 포함한 수 | 해당 없음 |  |
| 이미지당 추론 시간 |  |  |
| 사용 모델·checkpoint SHA-256 |  |  |

PatchCore의 장점은 위치 근거와 normal-only 학습 가능성입니다. 이것만으로 기존
YOLO 분류보다 좋다고 채택하지 않습니다. 같은 데이터, 같은 split, 같은 장비의
결과가 채택 근거입니다.

## 6. 현재 완료 경계

`docs/PATCHCORE_ANOMALY_PILOT_20260731.md`의 bounded pilot은 다음을
완료했습니다.

- selectable PatchCore 모델 프로필;
- normal-only 학습 준비 및 export;
- bundled PyTorch worker의 실제 GPU 학습·정상/이상 추론;
- image decision, raw score, threshold, 위치 후보, heatmap 계약;
- C# TCP 파싱과 미확정 후보 표시 문구;
- focused build/test.

이 공통 목적 흐름의 집중 회귀 검증은
`LabelingApplication.Tests.exe --wpf-anomaly-purpose-flow`로 실행합니다.
기존 검토·분류 학습·런타임 경계는 각각 다음 집중 게이트로 보호합니다.

- `LabelingApplication.Tests.exe --wpf-anomaly-queue-focus`
- `LabelingApplication.Tests.exe --anomaly-classification-training-workflow`
- `LabelingApplication.Tests.exe --wpf-yolov8-anomaly-classification-runtime-smoke`

아직 완료되지 않은 것은 생산 결함 정확도, 장시간 안정성, 모델별 held-out 비교,
깨끗한 GPU 장비에서의 패키지 검증입니다.
