# WPF Manual Smoke Checklist

Current image queue rule: one click on a row must open that image in the Main canvas, the selected row must be clearly highlighted, and the old top preview block should not be visible.

Portable project archive check:

1. With a dirty current image, `설정/도구 -> 프로젝트 이동 -> 프로젝트
   아카이브 내보내기` must stop and direct the operator to `라벨 저장`.
2. With an unconfirmed AI candidate, export/import must stop without confirming
   or saving the candidate.
3. After explicit label and Recipe saves, export a ZIP outside the Recipe and
   dataset folders and reopen it with the import action.
4. Select a new dataset parent. Import must create a new Recipe and dataset,
   preserve classes/splits/labels/object metadata, and leave `적용` explicit.
5. Repeating the same import must fail without overwriting either target.
6. External Python/model/weight references listed after import must be
   rechecked on the receiving machine.

Crash recovery check:

1. Edit one current image and confirm `라벨 저장 필요`.
2. End the process abnormally without using the application close dialog.
3. Restart with the same Recipe and dataset. The recovery dialog must name the
   Recipe, image, draft time/reason, and object counts.
4. Press `편집 복구`. Geometry and persistent Object Review metadata must
   return, pending AI/Smart Mask candidates must not return, and the state must
   remain unsaved.
5. Press `라벨 저장`, restart normally, and confirm that the recovery dialog
   no longer appears.
6. Repeat with `초안 폐기`; the draft must not appear on the next launch.
7. A changed/missing source image, wrong Recipe/dataset, corrupt checksum, or
   draft older than seven days must fail closed without changing label files.

WPF 화면을 직접 띄워 보는 날에는 아래 순서만 확인합니다.

## 실행

```powershell
.\scripts\start-labeling-workbench.ps1 -AppMode Debug
```

Release publish 산출물을 볼 때는 먼저 publish 후 실행합니다.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\publish-win-x64.ps1 -Configuration Release
.\scripts\start-labeling-workbench.ps1 -AppMode Publish
```

Publish 실행 파일은
`artifacts\publish\Release\win-x64\0.1.0\OpenVisionLab.LabelingStudio.exe`이며,
같은 폴더의 `release-manifest.json` 검증을 통과한 산출물만 smoke 대상으로 사용합니다.

## 화면 확인

1. 시작 직후 중앙 캔버스에 샘플 이미지가 보이고, 추론은 자동 실행되지 않아야 합니다.
2. 오른쪽 이미지 큐에서 한 번 클릭하면 선택 행이 강조되고 중앙 캔버스도 같은 이미지로 바뀌어야 합니다.
3. 워크플로 패널을 접거나 열고 폭을 바꾸면 중앙 이미지가 새 캔버스 영역에 자동 맞춤되어야 합니다. 이를 복구하기 위해 `맞춤`을 다시 누르면 안 됩니다.
4. `테마` 버튼으로 다크/라이트 전환 시 버튼, 패널, 로그, 상태바 글자가 읽혀야 합니다.
5. `추론 검토` 모드에서 `현재 추론`을 누르면 상태가 준비, worker 연결, 요청, 완료 순서로 바뀌어야 합니다.
6. 추론 후보는 캔버스와 오른쪽 `후보` 탭에 같이 보여야 합니다.
7. 후보 선택 시 클래스, 신뢰도, 좌표, 현재 라벨과의 겹침이 보여야 합니다.
8. 현재 라벨과 많이 겹치는 후보는 `중복 가능`으로 보이고 `확정`이 비활성화되어야 합니다.
9. `전체 확정`은 확정 가능한 후보만 저장하고 중복 후보는 건너뛰어야 합니다.
10. 확대/축소 후 추론 박스와 라벨이 이미지 위치를 따라가야 하며, 화면 밖 후보 라벨만 따로 남으면 안 됩니다.
11. `저장` 후 YOLO label 파일과 이미지 큐의 라벨/AI 상태가 갱신되어야 합니다.
12. `YOLO` 탭의 `첫 점검`, `테스트`, `재시작`, `중지` 버튼이 현재 상태에 맞게 활성화되어야 합니다.

## 세그멘테이션 자동 윤곽 확인

1. 세그멘테이션 Recipe에서 `라벨링 옵션 · 자동 윤곽: 꺼짐`을 한 번 눌러 켭니다.
2. 옵션을 켜면 박스 도구가 선택되지만 추론은 아직 실행되지 않아야 합니다.
3. 객체를 사각형으로 감싸면 별도 Smart Mask 시작 버튼 없이 MobileSAM 후보 생성이 바로 시작되어야 합니다.
4. 생성 중에는 새 박스를 겹쳐 그릴 수 없어야 합니다.
5. 후보는 자동 저장되지 않으며 `확정` 또는 `스킵`을 명시적으로 선택해야 합니다.
6. 확정 또는 스킵 후 `자동 윤곽: 켜짐`이 유지되고 다음 객체 박스를 바로 그릴 수 있어야 합니다.
7. Recipe를 닫았다 다시 열어도 선택이 복원되어야 하지만, 복원만으로 추론이 실행되면 안 됩니다.
8. 옵션을 끄면 이후 사각형은 일반 박스 라벨로 남아야 합니다.

## 자동 확인

```powershell
dotnet run --project .\tests\LabelingApplication.Tests\LabelingApplication.Tests.csproj -c Debug
.\scripts\smoke-yolo-workflow.ps1
.\scripts\verify-first-run.ps1 -SkipBuild -SkipTests -SkipYoloSmoke -RunPublishWpfSmoke
```

## 기록

확인한 결과는 `docs\WORK_TRACKING.md`의 완료 항목에 번호를 이어서 남깁니다.
