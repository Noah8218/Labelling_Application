using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;

namespace MvcVisionSystem.Yolo
{
    internal static class AnnotationFilePersistence
    {
        [ThreadStatic]
        private static AnnotationFileTransaction currentTransaction;

        public static bool ExecuteTransaction(Func<bool> saveFiles)
        {
            ArgumentNullException.ThrowIfNull(saveFiles);
            if (currentTransaction != null)
            {
                return saveFiles();
            }

            var transaction = new AnnotationFileTransaction();
            currentTransaction = transaction;
            try
            {
                bool shouldCommit;
                try
                {
                    shouldCommit = saveFiles();
                }
                catch (Exception saveFailure)
                {
                    try
                    {
                        transaction.Rollback();
                    }
                    catch (Exception rollbackFailure)
                    {
                        throw new AggregateException(
                            "Annotation save failed and its file rollback was incomplete.",
                            saveFailure,
                            rollbackFailure);
                    }

                    ExceptionDispatchInfo.Capture(saveFailure).Throw();
                    throw;
                }

                if (!shouldCommit)
                {
                    transaction.Rollback();
                    return false;
                }

                transaction.Commit();
                return true;
            }
            finally
            {
                currentTransaction = null;
            }
        }

        public static void ExecuteTransaction(Action saveFiles)
        {
            ArgumentNullException.ThrowIfNull(saveFiles);
            ExecuteTransaction(() =>
            {
                saveFiles();
                return true;
            });
        }

        public static void WriteAtomically(string path, Action<string> writeTemporaryFile)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Annotation path is required.", nameof(path));
            }

            ArgumentNullException.ThrowIfNull(writeTemporaryFile);

            string fullPath = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(fullPath)
                ?? throw new InvalidOperationException("Annotation path has no parent directory.");
            Directory.CreateDirectory(directory);

            string temporaryPath = Path.Combine(
                directory,
                $".{Path.GetFileName(fullPath)}.tmp-{Guid.NewGuid():N}");
            try
            {
                writeTemporaryFile(temporaryPath);
                using (var stream = new FileStream(
                    temporaryPath,
                    FileMode.Open,
                    FileAccess.ReadWrite,
                    FileShare.None))
                {
                    stream.Flush(flushToDisk: true);
                }

                if (currentTransaction != null)
                {
                    currentTransaction.Write(temporaryPath, fullPath);
                }
                else
                {
                    ReplaceOrMove(temporaryPath, fullPath, backupPath: null);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        public static void Delete(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            string fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath))
            {
                return;
            }

            if (currentTransaction != null)
            {
                currentTransaction.Delete(fullPath);
                return;
            }

            File.Delete(fullPath);
        }

        private static void ReplaceOrMove(string sourcePath, string destinationPath, string backupPath)
        {
            if (File.Exists(destinationPath))
            {
                File.Replace(sourcePath, destinationPath, backupPath, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(sourcePath, destinationPath);
            }
        }

        private sealed class AnnotationFileTransaction
        {
            private readonly Dictionary<string, TransactionEntry> entries =
                new Dictionary<string, TransactionEntry>(StringComparer.OrdinalIgnoreCase);
            private readonly List<TransactionEntry> order = new List<TransactionEntry>();

            public void Write(string temporaryPath, string targetPath)
            {
                if (entries.TryGetValue(targetPath, out _))
                {
                    ReplaceOrMove(temporaryPath, targetPath, backupPath: null);
                    return;
                }

                bool originalExists = File.Exists(targetPath);
                string backupPath = originalExists ? CreateBackupPath(targetPath) : string.Empty;
                var entry = new TransactionEntry(targetPath, backupPath, originalExists);
                entries.Add(targetPath, entry);
                order.Add(entry);
                ReplaceOrMove(temporaryPath, targetPath, originalExists ? backupPath : null);
            }

            public void Delete(string targetPath)
            {
                if (entries.ContainsKey(targetPath))
                {
                    if (File.Exists(targetPath))
                    {
                        File.Delete(targetPath);
                    }

                    return;
                }

                if (!File.Exists(targetPath))
                {
                    return;
                }

                string backupPath = CreateBackupPath(targetPath);
                var entry = new TransactionEntry(targetPath, backupPath, originalExists: true);
                entries.Add(targetPath, entry);
                order.Add(entry);
                File.Move(targetPath, backupPath);
            }

            public void Commit()
            {
                foreach (TransactionEntry entry in order)
                {
                    TryDeleteBackup(entry.BackupPath);
                }
            }

            public void Rollback()
            {
                var failures = new List<Exception>();
                for (int index = order.Count - 1; index >= 0; index--)
                {
                    TransactionEntry entry = order[index];
                    try
                    {
                        if (!entry.OriginalExists)
                        {
                            if (File.Exists(entry.TargetPath))
                            {
                                File.Delete(entry.TargetPath);
                            }

                            continue;
                        }

                        if (!File.Exists(entry.BackupPath))
                        {
                            continue;
                        }

                        ReplaceOrMove(entry.BackupPath, entry.TargetPath, backupPath: null);
                    }
                    catch (Exception ex)
                    {
                        failures.Add(ex);
                    }
                }

                if (failures.Count > 0)
                {
                    throw new AggregateException("One or more annotation files could not be rolled back.", failures);
                }
            }

            private static string CreateBackupPath(string targetPath)
            {
                string directory = Path.GetDirectoryName(targetPath)
                    ?? throw new InvalidOperationException("Annotation path has no parent directory.");
                return Path.Combine(
                    directory,
                    $".{Path.GetFileName(targetPath)}.rollback-{Guid.NewGuid():N}");
            }

            private static void TryDeleteBackup(string backupPath)
            {
                if (string.IsNullOrWhiteSpace(backupPath) || !File.Exists(backupPath))
                {
                    return;
                }

                try
                {
                    File.Delete(backupPath);
                }
                catch (IOException)
                {
                    // A committed canonical file remains valid even if cleanup must be retried later.
                }
                catch (UnauthorizedAccessException)
                {
                    // A committed canonical file remains valid even if cleanup must be retried later.
                }
            }
        }

        private sealed class TransactionEntry
        {
            public TransactionEntry(string targetPath, string backupPath, bool originalExists)
            {
                TargetPath = targetPath;
                BackupPath = backupPath;
                OriginalExists = originalExists;
            }

            public string TargetPath { get; }
            public string BackupPath { get; }
            public bool OriginalExists { get; }
        }
    }
}
