using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MvcVisionSystem
{
    /// <summary>
    /// Performs the non-visual Core transitions for the active recipe. The
    /// shell remains responsible for UI refresh and operator-facing status.
    /// </summary>
    public sealed class WpfProjectRecipeSessionService
    {
        private readonly SemaphoreSlim applyGate = new SemaphoreSlim(1, 1);
        private long applyRequestVersion;

        public string Save(CData data, string recipeName)
        {
            CRecipe.InitDirectory(recipeName);
            data.SaveConfig(recipeName);
            return WpfProjectRecipeService.BuildConfigPath(
                WpfProjectRecipeService.GetRecipeRootDirectory(),
                recipeName);
        }

        public string Apply(CGlobal application, string recipeName)
        {
            ValidateApplyInputs(application, recipeName);
            long requestVersion = Interlocked.Increment(ref applyRequestVersion);
            applyGate.Wait();
            try
            {
                CData loadedData = LoadRecipeData(application, recipeName);
                return CommitIfCurrent(application, recipeName, loadedData, requestVersion);
            }
            finally
            {
                applyGate.Release();
            }
        }

        public async Task<string> ApplyAsync(
            CGlobal application,
            string recipeName,
            CancellationToken cancellationToken = default)
        {
            ValidateApplyInputs(application, recipeName);
            long requestVersion = Interlocked.Increment(ref applyRequestVersion);
            await applyGate.WaitAsync(cancellationToken);
            try
            {
                CData sourceData = GetSourceData(application, recipeName);
                CData loadedData = await Task.Run(
                    () => LoadRecipeData(sourceData, recipeName),
                    cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                return CommitIfCurrent(application, recipeName, loadedData, requestVersion);
            }
            finally
            {
                applyGate.Release();
            }
        }

        public string ApplyPrepared(CGlobal application, string recipeName, CData preparedData)
        {
            ValidateApplyInputs(application, recipeName);
            ArgumentNullException.ThrowIfNull(preparedData);
            long requestVersion = Interlocked.Increment(ref applyRequestVersion);
            applyGate.Wait();
            try
            {
                return CommitIfCurrent(application, recipeName, preparedData, requestVersion);
            }
            finally
            {
                applyGate.Release();
            }
        }

        private static CData LoadRecipeData(CGlobal application, string recipeName)
        {
            CData sourceData = GetSourceData(application, recipeName);
            return LoadRecipeData(sourceData, recipeName);
        }

        private static CData GetSourceData(CGlobal application, string recipeName)
        {
            ValidateApplyInputs(application, recipeName);
            return application.Data ?? new CData();
        }

        private static CData LoadRecipeData(CData sourceData, string recipeName)
        {
            string normalizedRecipeName = recipeName.Trim();
            if (!CRecipe.InitDirectory(normalizedRecipeName))
            {
                throw new IOException($"Recipe directory could not be initialized: {normalizedRecipeName}");
            }

            return sourceData.LoadConfig(normalizedRecipeName)
                ?? throw new InvalidOperationException($"Recipe configuration could not be loaded: {normalizedRecipeName}");
        }

        private string CommitIfCurrent(
            CGlobal application,
            string recipeName,
            CData loadedData,
            long requestVersion)
        {
            if (requestVersion != Volatile.Read(ref applyRequestVersion))
            {
                throw new OperationCanceledException("A newer Recipe apply request replaced this request.");
            }

            return Commit(application, recipeName.Trim(), loadedData);
        }

        private static string Commit(CGlobal application, string recipeName, CData loadedData)
        {
            string previousRecipeName = (application.Recipe.Name ?? string.Empty).Trim();
            application.Data = loadedData;
            application.Recipe.CommitLoadedRecipe(recipeName);
            return previousRecipeName;
        }

        private static void ValidateApplyInputs(CGlobal application, string recipeName)
        {
            ArgumentNullException.ThrowIfNull(application);
            if (string.IsNullOrWhiteSpace(recipeName))
            {
                throw new ArgumentException("A Recipe name is required.", nameof(recipeName));
            }
        }
    }
}
