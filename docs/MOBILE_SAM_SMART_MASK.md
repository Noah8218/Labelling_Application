# MobileSAM 대화형 스마트 마스크

세그멘테이션 라벨링에서 결함을 완전히 브러시로 칠하는 대신, 시작
사각형과 포함점/제외점으로 로컬 MobileSAM 후보 외곽선을 만들고
반복 보정할 수 있습니다.

## 사용 순서

1. 데이터셋 목적을 `세그멘테이션`으로 선택합니다.
2. 결함 클래스를 선택합니다.
3. 사각형 도구로 결함을 여유 있게 감쌉니다.
4. 캔버스 상단의 `박스 → 스마트 마스크`를 누릅니다.
5. 파란 후보 외곽선을 확인합니다.
6. 빠진 영역에는 `+ 포함점`, 잘못 포함된 영역에는 `− 제외점`을
   선택하고 캔버스를 클릭합니다.
7. `점 취소` 또는 `점 지우기`로 프롬프트를 고치고, `경계`에서
   `빠름(48)`, `균형(96)`, `상세(256)` 중 하나를 고른 뒤
   `후보 다시 생성`을 누릅니다. 재실행은 대기 후보를 쌓지 않고
   현재 후보 하나를 교체합니다.
8. 맞으면 `확정`, 틀리면 `스킵`합니다. 후보 생성 중에는
   `생성 취소`로 worker를 중단할 수 있으며 현재 프롬프트는 유지됩니다.
9. 확정 또는 스킵 뒤 `다음 객체`를 누르면 박스 입력 상태로 돌아갑니다.
10. `확정`은 기존 정답 저장 흐름까지 실행합니다. 이후 수동으로 다시
    편집했다면 `라벨 저장`을 눌러 최신 상태를 기록합니다.

후보 생성만으로 정답이 저장되지는 않으며 `확정`할 때 저장됩니다.
실행 중 이미지, Recipe, 박스 또는 보정점 generation이 바뀌면 이전
결과를 적용하지 않습니다. 이미 확정한 라벨도 지우지 않습니다.

보라색은 시작 박스, 초록색은 포함점, 빨간색은 제외점, 파란색은 아직
확정하지 않은 후보 경계입니다.

## 로컬 실행 조건

- 등록된 로컬 Ultralytics 실행기와 같은 런타임을 재사용합니다.
- 해당 런타임 루트에 `mobile_sam.pt`가 있어야 합니다.
- 앱에 포함된 box+point 프롬프트 worker를 사용합니다.
- 네트워크 호출이나 자동 가중치 다운로드는 하지 않습니다.

현재 제품 슬라이스는 객체 하나마다 **단일 시작 박스와 여러
positive/negative point**를 사용합니다. 텍스트 프롬프트, 자동 객체
분리, video tracking은 제공하지 않습니다.

- [Ultralytics SAM 사용법](https://docs.ultralytics.com/models/sam/)
- [Ultralytics MobileSAM 문서](https://github.com/ultralytics/ultralytics/blob/main/docs/en/models/mobile-sam.md)

## 8개 결함 클래스 고정 평가

합성 결함 8종의 `train`, `val`, `test`에서 클래스별 1장씩 고정한 24개
표본을 실제 MobileSAM으로 실행했습니다. 정답 메타데이터의 tight box를
프롬프트로 사용했을 때 24/24가 IoU `0.50` 이상이었고, 전체 중앙 IoU는
`0.8562`, worker P95 실행 시간은 `3168.4 ms`였습니다. 가장 낮은 클래스인
`crack`도 중앙 IoU `0.7129`로 완료 기준을 통과했습니다.

이 결과와 후속 96-call box-jitter matrix는 box-only 입력이 유지되는지
보호하는 회귀 기준입니다. 2026-07-27 P0-B는 이 호환 경로를 유지하면서
point correction을 별도 session으로 확장했습니다. 상세 선택 규칙,
클래스별 결과와 SHA-256은
[8개 결함 클래스 사용성 평가](MOBILE_SAM_8_CLASS_USABILITY_MATRIX_20260722.md)에
기록했습니다.

## 2026-07-27 point correction 근거

실제 MobileSAM 합성 fixture에서 다음을 확인했습니다.

- 시작 box mask area: `4431`
- positive point 적용: `6529` (`+2098`)
- 넓은 positive 결과: `44512`
- positive+negative 결과: `22399`
- negative point가 제거한 영역: `22113` pixel (`49.6787%`)
- 빠름 상세도: polygon `48` points
- runtime: MobileSAM / Ultralytics `8.4.101` / Torch `2.12.1+cpu` / CPU
- weight SHA-256:
  `6DBB90523A35330FEDD7F1D3DFC66F995213D81B29A5CA8108DBCDD4E37D6C2F`
- source image SHA-256: 실행 전후 동일

증거:

- `artifacts\mobile-sam-point-correction\20260727-185324\point-correction-evidence.json`
- `artifacts\ui\smart-mask-p0b-20260727\after-actual-exe-before-confirm-1920x1080.png`
- `artifacts\ui\smart-mask-p0b-20260727\after-actual-exe-after-confirm-1920x1080.png`

## 완료 범위와 한계

- 완료: 실제 이미지 박스 입력 → 포함/제외점 보정 → rerun-replace →
  사람 검토·확정/스킵 → 다음 객체 → 기존 canonical 세그먼트
  JSON·마스크 PNG 저장.
- 완료: point undo/clear, 48/96/256 polygon detail, worker cancellation,
  image/Recipe/prompt stale-result 차단.
- 완료: 원본 이미지 불변성, 모델 가중치 SHA-256, Python/Ultralytics/Torch/장치 provenance 기록.
- 제외: 텍스트/음성 프롬프트, 여러 객체 자동 분리, video tracking,
  모델 학습, 자동 확정·저장, 자동 weight download.
- 증거 경계: 합성 결함 이미지로 기능 재현성을 검증했으며 생산 카메라 정확도는 평가하지 않았습니다.
- 증거 경계: 8개 클래스 평가는 정답 메타데이터의 정확한 박스를 사용했으며,
  box-jitter matrix는 선언한 작은 오차 범위만 증명합니다.
