using System;
using System.IO;
using System.Linq;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        private readonly WpfPortableProjectArchiveService portableProjectArchiveService =
            new WpfPortableProjectArchiveService();

        private void ExecuteExportProjectArchiveCommand()
        {
            if (isApplicationCloseApproved)
            {
                return;
            }

            string recipeName = GetCurrentRecipeName();
            string configPath = GetCurrentRecipeConfigPath();
            string datasetRoot = global.Data?.OutputRootPath ?? string.Empty;
            WpfProjectArchivePreflightResult preflight = WpfProjectArchivePreflightService.Check(
                WpfProjectArchiveOperation.Export,
                BuildProjectArchiveOperationState(),
                recipeName,
                configPath,
                datasetRoot);
            if (!preflight.CanProceed)
            {
                SetProjectConfigStatus(preflight.StatusText);
                return;
            }

            string suggestedPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                recipeName + ".ovl-project.zip");
            if (!fileDialogService.TryPickSaveFile(
                this,
                "프로젝트 아카이브 내보내기",
                "OpenVisionLab 프로젝트 (*.ovl-project.zip)|*.ovl-project.zip|ZIP 아카이브 (*.zip)|*.zip",
                suggestedPath,
                ".zip",
                out string archivePath))
            {
                return;
            }

            if (isApplicationCloseApproved)
            {
                return;
            }

            try
            {
                WpfProjectArchiveExportResult result = portableProjectArchiveService.Export(
                    recipeName,
                    GetCurrentRecipeConfigDirectory(),
                    datasetRoot,
                    archivePath);
                string referenceText = result.ExternalReferenceCount > 0
                    ? $" / 외부 참조 {result.ExternalReferenceCount}개는 경로만 기록"
                    : string.Empty;
                SetProjectConfigStatus(
                    $"프로젝트 아카이브 완료: {Path.GetFileName(result.ArchivePath)} / 파일 {result.FileCount}개{referenceText}");
                AppendLog($"프로젝트 아카이브 내보내기: {result.ArchivePath}");
            }
            catch (Exception ex)
            {
                SetProjectConfigStatus("프로젝트 아카이브 내보내기 실패: " + ex.Message);
                AppendLog("프로젝트 아카이브 내보내기 실패: " + ex.Message);
            }
        }

        private void ExecuteImportProjectArchiveCommand()
        {
            if (isApplicationCloseApproved)
            {
                return;
            }

            WpfProjectArchivePreflightResult preflight = WpfProjectArchivePreflightService.Check(
                WpfProjectArchiveOperation.Import,
                BuildProjectArchiveOperationState());
            if (!preflight.CanProceed)
            {
                SetProjectConfigStatus(preflight.StatusText);
                return;
            }

            if (!TryPickFile(
                "프로젝트 아카이브 가져오기",
                "OpenVisionLab 프로젝트 (*.ovl-project.zip;*.zip)|*.ovl-project.zip;*.zip",
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                out string archivePath))
            {
                return;
            }

            string defaultDatasetParent = ResolveProjectArchiveDatasetParent();
            if (!TryPickFolder(
                "가져온 데이터셋을 만들 상위 폴더 선택",
                defaultDatasetParent,
                out string datasetParent))
            {
                return;
            }

            if (isApplicationCloseApproved)
            {
                return;
            }

            try
            {
                WpfProjectArchiveImportResult result = portableProjectArchiveService.Import(
                    archivePath,
                    GetRecipeRootDirectory(),
                    datasetParent);
                PopulateProjectRecipeList(result.RecipeName);
                ProjectConfigViewModel?.SelectRecipeFromList(result.RecipeName);
                string referenceText = result.ExternalReferenceCount > 0
                    ? $" 외부 실행기/가중치 참조 {result.ExternalReferenceCount}개는 이 PC에서 다시 확인해야 합니다."
                    : string.Empty;
                SetProjectConfigStatus(
                    $"가져오기 완료: {result.RecipeName}. 자동 적용하지 않았습니다. 목록에서 `적용`을 누르세요.{referenceText}");
                AppendLog(
                    $"프로젝트 아카이브 가져오기: {result.ArchivePath} -> {result.RecipeDirectory} / {result.DatasetRootPath}");
            }
            catch (Exception ex)
            {
                SetProjectConfigStatus("프로젝트 아카이브 가져오기 실패: " + ex.Message);
                AppendLog("프로젝트 아카이브 가져오기 실패: " + ex.Message);
            }
        }

        private WpfApplicationCloseState BuildProjectArchiveOperationState()
        {
            return new WpfApplicationCloseState
            {
                HasUnsavedAnnotations =
                    !string.IsNullOrWhiteSpace(annotationDirtyReason)
                    || HasPendingMaskStrokeCommitWork(),
                UnsavedAnnotationReason = annotationDirtyReason,
                PendingCandidateCount = candidateReviewState.PendingCount,
                ActiveWorkNames = GetActiveApplicationCloseWorkNames(),
                ActiveImagePath = activeImagePath
            };
        }

        private string ResolveProjectArchiveDatasetParent()
        {
            string outputRoot = global.Data?.OutputRootPath ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(outputRoot))
            {
                string parent = Path.GetDirectoryName(outputRoot.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar));
                if (!string.IsNullOrWhiteSpace(parent) && Directory.Exists(parent))
                {
                    return parent;
                }
            }

            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }
    }
}
