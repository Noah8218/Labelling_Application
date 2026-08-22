using System;
using System.Collections.Generic;
using System.Linq;

namespace MvcVisionSystem._1._Core
{
    public sealed class PythonEnvironmentCheckResult
    {
        public string PythonExecutablePath { get; set; } = string.Empty;

        public string RequirementsPath { get; set; } = string.Empty;

        public IReadOnlyList<string> RequiredPackages { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> MissingPackages { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();

        public bool IsReady => Errors.Count == 0 && MissingPackages.Count == 0;

        public string Summary
        {
            get
            {
                if (Errors.Count > 0)
                {
                    return Errors[0];
                }

                if (MissingPackages.Count > 0)
                {
                    return $"누락 패키지: {string.Join(", ", MissingPackages.Take(6))}";
                }

                return Warnings.Count > 0 ? Warnings[0] : "Python 실행 환경 준비 완료.";
            }
        }
    }

    public sealed class PythonPackageInstallResult
    {
        public bool Succeeded { get; set; }

        public int ExitCode { get; set; }

        public string CommandLine { get; set; } = string.Empty;

        public string Output { get; set; } = string.Empty;

        public string Error { get; set; } = string.Empty;

        public string Summary => Succeeded
            ? "Python requirements 설치 완료."
            : !string.IsNullOrWhiteSpace(Error) ? Error : Output;
    }
}
