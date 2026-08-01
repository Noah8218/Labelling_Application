# Documentation Index

이 문서는 `docs` 폴더의 탐색 허브입니다. 문서의 권위나 완료 상태를 새로
정의하지 않으며, 기존 파일을 이동하거나 역사 기록을 현재 사실처럼 바꾸지
않습니다.

## Start here

1. 현재 제품 정체성·성숙도·우선순위는
   [Current Product Status](CURRENT_PRODUCT_STATUS.md)를 읽습니다.
2. 다음 작업을 이어갈 때는 [Next Thread Handoff](NEXT_THREAD_HANDOFF.md)를
   읽습니다.
3. 완료되어 보호되는 동작은 [Stable Verified Areas](STABLE_VERIFIED_AREAS.md),
   시간순 상세 기록은 [Work Tracking](WORK_TRACKING.md)에서 확인합니다.
4. 코드 위치와 책임 소유자는 [Code Structure](CODE_STRUCTURE.md)를 따릅니다.

충돌 시 `AGENTS.md -> CURRENT_PRODUCT_STATUS.md -> STABLE_VERIFIED_AREAS.md ->
WORK_TRACKING.md -> 개별 날짜 문서` 순서가 우선합니다. 날짜가 붙은 감사·계획·
증거 문서는 당시 상태를 보존하는 기록이며, 최신 우선순위를 소유하지 않습니다.

## Lifecycle labels

- `CURRENT`: 현재 권위 또는 반복해서 사용하는 탐색 문서
- `GUIDE`: 운영자가 재사용하는 절차와 체크리스트
- `CONTRACT`: 구현·안전·제품 범위를 보호하는 계약 또는 완료 기록
- `EVIDENCE`: 특정 입력·버전·실행 결과에 한정된 검증 근거
- `HISTORY`: 이전 계획·감사·마이그레이션·커밋 맥락

## 1. Current authority and repository navigation

- `CURRENT` [Code Structure](CODE_STRUCTURE.md)
- `CURRENT` [Current Product Status](CURRENT_PRODUCT_STATUS.md)
- `CURRENT` [Labeling Program Direction](LABELING_PROGRAM_DIRECTION.md)
- `CURRENT` [Labeling Studio User-Centered Development Direction](LABELING_STUDIO_USER_CENTERED_DEVELOPMENT_DIRECTION_20260729.md)
- `CONTRACT` [Local Test Storage And Left-Monitor Contract](LOCAL_TEST_STORAGE_AND_LEFT_MONITOR_CONTRACT_20260731.md)
- `CURRENT` [Next Thread Handoff](NEXT_THREAD_HANDOFF.md)
- `CONTRACT` [Repository Artifact Inventory And Retention Policy](REPOSITORY_ARTIFACT_INVENTORY_AND_RETENTION_POLICY_20260731.md)
- `EVIDENCE` [Repository Cleanup Preview](REPOSITORY_CLEANUP_PREVIEW_20260731.md)
- `EVIDENCE` [Approved C-Drive Repository Candidate Cleanup](REPOSITORY_C_CANDIDATE_CLEANUP_EXECUTION_20260731.md)
- `CURRENT` [Stable Verified Areas](STABLE_VERIFIED_AREAS.md)
- `CURRENT` [Work Tracking](WORK_TRACKING.md)

## 2. Operator guides and repeatable procedures

- `GUIDE` [Anomaly Detection Flow](ANOMALY_DETECTION_FLOW.md)
- `GUIDE` [Industrial Dataset Preparation](INDUSTRIAL_DATASET_PREPARATION.md)
- `GUIDE` [KTEM Pretrained YOLO Integration](KTEM_PRETRAINED_YOLO_INTEGRATION.md)
- `GUIDE` [MobileSAM Smart Mask](MOBILE_SAM_SMART_MASK.md)
- `GUIDE` [Synthetic-First Evidence Contract](SYNTHETIC_EVIDENCE_CONTRACT.md)
- `GUIDE` [OpenVisionLab Labeling Studio 사용 가이드](tutorial/README.md)
- `GUIDE` [WPF Annotation Tool Validation](WPF_ANNOTATION_TOOL_VALIDATION.md)
- `GUIDE` [WPF Manual Smoke Checklist](WPF_MANUAL_SMOKE_CHECKLIST.md)
- `GUIDE` [YOLOv5 Training Result Workflow](YOLOV5_TRAINING_RESULT_WORKFLOW.md)

## 3. Productization, release, recovery, and external validation

- `CONTRACT` [CI Complete Regression Gate](CI_COMPLETE_REGRESSION_GATE_20260801.md)
- `CONTRACT` [Headless Environment Self-Test CLI](HEADLESS_ENVIRONMENT_CHECK_CLI_20260801.md)
- `CONTRACT` [P1-B Bounded Crash Recovery Journal](BOUNDED_CRASH_RECOVERY_P1B_20260730.md)
- `EVIDENCE` [Commercial Readiness Audit](COMMERCIAL_READINESS_AUDIT_20260730.md)
- `EVIDENCE` [Engineering Release 0.1.1 Graphics Preflight Evidence](ENGINEERING_RELEASE_0_1_1_GRAPHICS_PREFLIGHT_EVIDENCE_20260731.md)
- `CONTRACT` [Field Data Intake Prerequisite](FIELD_DATA_INTAKE_PREREQUISITE_20260728.md)
- `EVIDENCE` [Independent Holdout Readiness](INDEPENDENT_HOLDOUT_READINESS_20260721.md)
- `CONTRACT` [Next Development Decision](NEXT_DEVELOPMENT_DECISION_20260730.md)
- `CONTRACT` [P0-C GPU-Capable Clean Target Validation Plan](P0C_GPU_CAPABLE_CLEAN_TARGET_VALIDATION_PLAN_20260731.md)
- `EVIDENCE` [P0-C Hyper-V Labeling Evidence](P0C_HYPERV_LABELING_EVIDENCE_20260731.md)
- `EVIDENCE` [P0-C Portable Sandbox Evidence](P0C_PORTABLE_SANDBOX_EVIDENCE_20260730.md)
- `GUIDE` [P0-C Windows Sandbox Setup](P0C_WINDOWS_SANDBOX_SETUP_20260730.md)
- `CONTRACT` [P0-B2 Packaged Runtime Diagnostics](PACKAGED_RUNTIME_DIAGNOSTICS_P0B2_20260730.md)
- `CONTRACT` [P1-A Portable Project Archive](PORTABLE_PROJECT_ARCHIVE_P1A_20260730.md)
- `CONTRACT` [P0-B1 Release Package Contract](RELEASE_PACKAGE_CONTRACT_P0B1_20260730.md)
- `CONTRACT` [P0-C Runtime Graphics Capability Preflight](RUNTIME_GRAPHICS_CAPABILITY_PREFLIGHT_P0C_20260731.md)
- `CONTRACT` [P0 Safe Application Close](SAFE_APPLICATION_CLOSE_P0_20260729.md)

## 4. Feature contracts and completion records

- `CONTRACT` [Batch AI Preflight P5-B](BATCH_AI_PREFLIGHT_P5B_20260728.md)
- `CONTRACT` [Canonical Class Index Visibility P1](CANONICAL_CLASS_INDEX_VISIBILITY_P1_20260729.md)
- `CONTRACT` [Dataset Health Class Filter](DATASET_HEALTH_CLASS_FILTER_20260729.md)
- `CONTRACT` [Dataset Health Split Filter P3](DATASET_HEALTH_SPLIT_FILTER_P3_20260729.md)
- `CONTRACT` [Dataset Interchange Preflight P5-A](DATASET_INTERCHANGE_PREFLIGHT_P5A_20260728.md)
- `CONTRACT` [Dataset Purpose Automatic Name Sync](DATASET_PURPOSE_AUTOMATIC_NAME_SYNC_20260722.md)
- `CONTRACT` [P3 Display-Only Image Aids](DISPLAY_ONLY_IMAGE_AIDS_P3_20260728.md)
- `CONTRACT` [Four-Point Extreme Box Product Contract](FOUR_POINT_EXTREME_BOX_CONTRACT_20260729.md)
- `CONTRACT` [Four-Point Extreme Box Implementation](FOUR_POINT_EXTREME_BOX_IMPLEMENTATION_20260729.md)
- `CONTRACT` [Image Queue Action Worklist](IMAGE_QUEUE_ACTION_WORKLIST_20260722.md)
- `CONTRACT` [P2 Intelligent Scissors](INTELLIGENT_SCISSORS_P2_20260728.md)
- `CONTRACT` [Model, Anomaly, and Comparison Review Slices](MODEL_ANOMALY_COMPARISON_REVIEW_SLICES.md)
- `CONTRACT` [PatchCore Normal-Only Anomaly Pilot](PATCHCORE_ANOMALY_PILOT_20260731.md)
- `CONTRACT` [PatchCore Heatmap Review View](PATCHCORE_HEATMAP_REVIEW_VIEW_20260801.md)
- `CONTRACT` [Object Detection MVP Completion](OBJECT_DETECTION_MVP_COMPLETION.md)
- `CONTRACT` [Object Review Contextual Group Controls](OBJECT_GROUP_CONTEXTUAL_CONTROLS_20260730.md)
- `CONTRACT` [Object Group Review Contract P5](OBJECT_GROUP_REVIEW_CONTRACT_P5_20260729.md)
- `CONTRACT` [Object Group Review Implementation P5](OBJECT_GROUP_REVIEW_IMPLEMENTATION_P5_20260729.md)
- `CONTRACT` [Object Review Persistent Metadata Consumer P4](OBJECT_METADATA_REVIEW_CONSUMER_P4_20260729.md)
- `CONTRACT` [Object Review Contextual UI Correction](OBJECT_REVIEW_CONTEXTUAL_UI_CORRECTION_20260728.md)
- `CONTRACT` [Object Session State P2](OBJECT_SESSION_STATE_P2_20260728.md)
- `CONTRACT` [P2 Polygon Vertex Insert/Delete](POLYGON_VERTEX_EDIT_P2_20260728.md)
- `CONTRACT` [Recipe Dataset Version v2](RECIPE_DATASET_VERSION_V2_20260723.md)
- `CONTRACT` [Segmentation Hole Editing P1-C](SEGMENTATION_HOLE_P1C_20260728.md)
- `CONTRACT` [Segmentation Interchange Preservation](SEGMENTATION_INTERCHANGE_PRESERVATION_CONTRACT_20260727.md)
- `CONTRACT` [Segmentation Merge/Join P1-C](SEGMENTATION_MERGE_P1C_20260727.md)
- `CONTRACT` [Segmentation Remove-Underlying P1-C](SEGMENTATION_REMOVE_UNDERLYING_P1C_20260728.md)
- `CONTRACT` [Segmentation Split/Slice P1-C](SEGMENTATION_SPLIT_P1C_20260727.md)
- `CONTRACT` [Segmentation UX Completion](SEGMENTATION_UX_COMPLETION.md)
- `CONTRACT` [Segmentation Z-Order P1-C](SEGMENTATION_ZORDER_P1C_20260728.md)
- `CONTRACT` [Smart Contour Auto Mode And Layout Auto-Fit](SMART_CONTOUR_AUTO_MODE_AND_LAYOUT_FIT_20260728.md)
- `CONTRACT` [Smart Mask Candidate Compare And Restore](SMART_MASK_CANDIDATE_COMPARE_RESTORE_20260728.md)
- `CONTRACT` [Smart Mask Contextual Correction UX](SMART_MASK_CONTEXTUAL_CORRECTION_UX_20260728.md)
- `CONTRACT` [Smart Mask Operator Documentation Truth](SMART_MASK_OPERATOR_DOCUMENTATION_TRUTH_P2_20260729.md)

## 5. Verification evidence, datasets, and performance analysis

- `EVIDENCE` [Actual EXE User Function Audit](ACTUAL_EXE_USER_FUNCTION_AUDIT_20260729.md)
- `EVIDENCE` [Beginner End-To-End UX Audit](BEGINNER_END_TO_END_UX_AUDIT_20260722.md)
- `EVIDENCE` [Circular Disk Synthetic 1,000-Image Evidence](CIRCULAR_DISK_SYNTHETIC_1000_EVIDENCE_20260720.md)
- `EVIDENCE` [Dataset Health Review Slice](DATASET_HEALTH_REVIEW_SLICE.md)
- `EVIDENCE` [Dataset Health Visual QA P4](DATASET_HEALTH_VISUAL_QA_P4_20260728.md)
- `EVIDENCE` [External Native YOLO Intake Review Slice](EXTERNAL_NATIVE_YOLO_INTAKE_REVIEW_SLICE.md)
- `EVIDENCE` [Floppy Disk And Hexagon Dataset Test](FLOPPY_DISK_AND_HEXAGON_DATASET_TEST_20260721.md)
- `EVIDENCE` [Image Queue 10K Review Slice](IMAGE_QUEUE_10K_REVIEW_SLICE.md)
- `EVIDENCE` [Labeling UX Benchmark](LABELING_UX_BENCHMARK.md)
- `EVIDENCE` [MobileSAM 8-Class Usability Matrix](MOBILE_SAM_8_CLASS_USABILITY_MATRIX_20260722.md)
- `EVIDENCE` [MobileSAM Box Jitter Matrix](MOBILE_SAM_BOX_JITTER_MATRIX_20260722.md)
- `EVIDENCE` [Segmentation E30 Confidence 0.25 Evidence](SEGMENTATION_E30_CONFIDENCE025_TEST_EVIDENCE_20260722.md)
- `EVIDENCE` [Segmentation E30 Error Analysis](SEGMENTATION_E30_ERROR_ANALYSIS_20260721.md)
- `EVIDENCE` [Segmentation E30 Three-Model Comparison](SEGMENTATION_E30_THREE_MODEL_COMPARISON_20260722.md)
- `EVIDENCE` [Smart Mask Correction Effectiveness](SMART_MASK_CORRECTION_EFFECTIVENESS_20260728.md)
- `EVIDENCE` [U-Net E30 Class Confusion Analysis](UNET_E30_CLASS_CONFUSION_ANALYSIS_20260722.md)
- `EVIDENCE` [U-Net Segmentation Adapter Design](UNET_SEGMENTATION_ADAPTER_DESIGN_20260721.md)
- `EVIDENCE` [WPF Annotation Object Verification](WPF_ANNOTATION_OBJECT_VERIFICATION.md)
- `EVIDENCE` [WPF Labeling Session Verification](WPF_LABELING_SESSION_VERIFICATION_20260622.md)
- `EVIDENCE` [WPF YOLO Training Session Verification](WPF_YOLO_TRAINING_SESSION_VERIFICATION_20260622.md)
- `EVIDENCE` [YOLO Model Comparison](YOLO_MODEL_COMPARISON_20260622.md)
- `EVIDENCE` [YOLO11 Anomaly Classification Prerequisite Audit](YOLO11_ANOMALY_CLASSIFICATION_PREREQUISITE_AUDIT_20260723.md)
- `EVIDENCE` [YOLO11 Engine Comparison](YOLO11_ENGINE_COMPARISON_20260721.md)
- `EVIDENCE` [YOLOv5 Real Workflow Verification](YOLOV5_REAL_WORKFLOW_VERIFICATION_20260626.md)

## 6. Historical plans, audits, and migration records

- `HISTORY` [Actual EXE Video And GitHub GIF Plan](ACTUAL_EXE_VIDEO_AND_GITHUB_GIF_PLAN_20260728.md)
- `HISTORY` [Commit Scope 2026-07-02](COMMIT_SCOPE_20260702.md)
- `HISTORY` [Refactoring Commit Scope 2026-07-26](COMMIT_SCOPE_20260726_REFACTORING.md)
- `HISTORY` [Current Worktree Integration Verification](CURRENT_WORKTREE_INTEGRATION_VERIFICATION_20260729.md)
- `HISTORY` [Labeling Editor Commercial Gap And Roadmap](LABELING_EDITOR_COMMERCIAL_GAP_AND_ROADMAP_20260727.md)
- `HISTORY` [Labeling Studio Commercial UX Gap Review](LABELING_STUDIO_COMMERCIAL_UX_GAP_REVIEW_20260710.md)
- `HISTORY` [Labeling Studio Commercial Video Review](LABELING_STUDIO_COMMERCIAL_VIDEO_REVIEW_20260727.md)
- `HISTORY` [Labeling Studio Completeness Audit](LABELING_STUDIO_COMPLETENESS_AUDIT.md)
- `HISTORY` [WPF Autonomous Progress](WPF_AUTONOMOUS_PROGRESS.md)
- `HISTORY` [WPF View Migration](WPF_VIEW_MIGRATION.md)

## Contribution checklist

1. 현재 권위를 바꿀 때만 `CURRENT_PRODUCT_STATUS.md`와 관련 탐색 문서를
   함께 갱신합니다.
2. 새 문서는 위 여섯 책임군 중 정확히 한 곳에 링크하고 상태 표기를 붙입니다.
3. 날짜 문서는 당시 입력·버전·명령·증거·경계를 유지합니다.
4. 기존 문서를 이동하거나 이름을 바꿔야 한다면 저장소 전체 참조를 먼저 찾고,
   별도 구조 변경으로 검증합니다.
5. 아래 검증을 실행합니다.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Test-DocumentationInformationArchitecture.ps1
```
