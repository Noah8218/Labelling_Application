using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;

namespace LabelingApplication.Tests;

internal static class ReleasePackageContractTests
{
    private const string ExpectedVersion = "0.1.0";
    private const string ExpectedAssemblyVersion = "0.1.0.0";
    private const string ExpectedSdkVersion = "8.0.421";

    internal static void TestReleasePackageContract()
    {
        string repoRoot = FindRepositoryRoot();
        string releaseDirectory = Path.Combine(
            repoRoot,
            "artifacts",
            "publish",
            "Release",
            "win-x64",
            ExpectedVersion);

        VerifySourceContract(repoRoot);
        VerifyPublishedPackage(repoRoot, releaseDirectory);

        ProcessResult initialVerification = RunVerifier(repoRoot);
        AssertTrue(
            initialVerification.ExitCode == 0,
            $"Release verifier rejected the untouched package.{Environment.NewLine}{initialVerification.Output}");

        string noticePath = Path.Combine(releaseDirectory, "NOTICE");
        byte[] originalNotice = File.ReadAllBytes(noticePath);
        try
        {
            using FileStream stream = new(noticePath, FileMode.Append, FileAccess.Write, FileShare.None);
            stream.WriteByte(0x0A);

            ProcessResult tamperedVerification = RunVerifier(repoRoot);
            AssertTrue(
                tamperedVerification.ExitCode != 0,
                "Release verifier must fail closed when a payload file is modified.");
        }
        finally
        {
            File.WriteAllBytes(noticePath, originalNotice);
        }

        ProcessResult restoredVerification = RunVerifier(repoRoot);
        AssertTrue(
            restoredVerification.ExitCode == 0,
            $"Release verifier did not accept the exactly restored package.{Environment.NewLine}{restoredVerification.Output}");
    }

    private static void VerifySourceContract(string repoRoot)
    {
        string globalJsonPath = Path.Combine(repoRoot, "global.json");
        using JsonDocument globalJson = JsonDocument.Parse(File.ReadAllText(globalJsonPath));
        string sdkVersion = globalJson.RootElement
            .GetProperty("sdk")
            .GetProperty("version")
            .GetString() ?? string.Empty;
        AssertEqual(ExpectedSdkVersion, sdkVersion, "global.json SDK version");
        string sdkRollForward = globalJson.RootElement
            .GetProperty("sdk")
            .GetProperty("rollForward")
            .GetString() ?? string.Empty;
        AssertEqual("disable", sdkRollForward, "global.json SDK roll-forward policy");

        XDocument buildProps = XDocument.Load(Path.Combine(repoRoot, "Directory.Build.props"));
        XElement propertyGroup = buildProps.Root?
            .Elements("PropertyGroup")
            .FirstOrDefault(group => group.Element("VersionPrefix") is not null)
            ?? throw new InvalidOperationException("Directory.Build.props does not declare VersionPrefix.");
        AssertEqual(ExpectedVersion, propertyGroup.Element("VersionPrefix")?.Value, "VersionPrefix");
        AssertEqual(ExpectedAssemblyVersion, propertyGroup.Element("AssemblyVersion")?.Value, "AssemblyVersion");
        AssertEqual("true", propertyGroup.Element("Deterministic")?.Value, "Deterministic build policy");
        AssertEqual(
            "false",
            propertyGroup.Element("IncludeSourceRevisionInInformationalVersion")?.Value,
            "informational-version source revision policy");

        string assemblyInfo = File.ReadAllText(Path.Combine(repoRoot, "Properties", "AssemblyInfo.cs"));
        AssertTrue(
            !assemblyInfo.Contains("1.0.*", StringComparison.Ordinal),
            "Legacy wildcard assembly version must not remain in AssemblyInfo.cs.");

        string appProject = File.ReadAllText(Path.Combine(repoRoot, "OpenVisionLab.LabelingStudio.csproj"));
        AssertTrue(
            !appProject.Contains("<Deterministic>false</Deterministic>", StringComparison.Ordinal),
            "The application project must not disable deterministic output.");
        AssertTrue(
            appProject.Contains("THIRD-PARTY-NOTICES.txt", StringComparison.Ordinal),
            "The application project must publish third-party notices.");

        string publishScript = File.ReadAllText(Path.Combine(repoRoot, "scripts", "publish-win-x64.ps1"));
        foreach (string requiredToken in new[]
        {
            "ReleaseVersion",
            "FrameworkDependent",
            "VerifyOnly",
            "release-manifest.json",
            "Get-FileHash",
            "SHA256"
        })
        {
            AssertTrue(
                publishScript.Contains(requiredToken, StringComparison.Ordinal),
                $"Publish script is missing contract token: {requiredToken}");
        }
    }

    private static void VerifyPublishedPackage(string repoRoot, string releaseDirectory)
    {
        AssertTrue(Directory.Exists(releaseDirectory), $"Release directory is missing: {releaseDirectory}");

        string manifestPath = Path.Combine(releaseDirectory, "release-manifest.json");
        string textManifestPath = Path.Combine(releaseDirectory, "publish-manifest.txt");
        AssertTrue(File.Exists(manifestPath), "release-manifest.json is missing.");
        AssertTrue(File.Exists(textManifestPath), "publish-manifest.txt is missing.");

        using JsonDocument manifestDocument = JsonDocument.Parse(File.ReadAllText(manifestPath));
        JsonElement manifest = manifestDocument.RootElement;
        AssertTrue(manifest.GetProperty("schemaVersion").GetInt32() == 1, "Manifest schema must be version 1.");
        AssertEqual(ExpectedVersion, manifest.GetProperty("productVersion").GetString(), "manifest product version");
        AssertEqual(
            ExpectedAssemblyVersion,
            manifest.GetProperty("assemblyVersion").GetString(),
            "manifest assembly version");
        AssertEqual(
            ExpectedAssemblyVersion,
            manifest.GetProperty("fileVersion").GetString(),
            "manifest file version");

        JsonElement build = manifest.GetProperty("build");
        AssertEqual(ExpectedSdkVersion, build.GetProperty("sdkVersion").GetString(), "manifest SDK version");
        AssertEqual("Release", build.GetProperty("configuration").GetString(), "manifest configuration");
        AssertEqual("win-x64", build.GetProperty("runtimeIdentifier").GetString(), "manifest RID");
        AssertTrue(build.GetProperty("selfContained").GetBoolean(), "Release package must be self-contained.");

        foreach (string requiredFile in new[]
        {
            "OpenVisionLab.LabelingStudio.exe",
            "OpenVisionLab.LabelingStudio.dll",
            "hostfxr.dll",
            "LICENSE",
            "NOTICE",
            "THIRD-PARTY-NOTICES.txt"
        })
        {
            AssertTrue(
                File.Exists(Path.Combine(releaseDirectory, requiredFile)),
                $"Required release file is missing: {requiredFile}");
        }

        Dictionary<string, ManifestEntry> manifestFiles =
            new(StringComparer.OrdinalIgnoreCase);
        foreach (JsonElement fileElement in manifest.GetProperty("files").EnumerateArray())
        {
            string relativePath = fileElement.GetProperty("path").GetString() ?? string.Empty;
            AssertTrue(!string.IsNullOrWhiteSpace(relativePath), "Manifest contains an empty file path.");
            AssertTrue(!relativePath.Contains('\\'), $"Manifest path is not normalized: {relativePath}");
            AssertTrue(
                manifestFiles.TryAdd(
                    relativePath,
                    new ManifestEntry(
                        fileElement.GetProperty("length").GetInt64(),
                        fileElement.GetProperty("sha256").GetString() ?? string.Empty)),
                $"Manifest contains a duplicate path: {relativePath}");
        }

        string[] actualPayloadPaths = Directory
            .EnumerateFiles(releaseDirectory, "*", SearchOption.AllDirectories)
            .Where(path =>
                !string.Equals(Path.GetFileName(path), "release-manifest.json", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(Path.GetFileName(path), "publish-manifest.txt", StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(releaseDirectory, path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        AssertTrue(
            actualPayloadPaths.Length == manifestFiles.Count,
            $"Manifest payload count mismatch: manifest={manifestFiles.Count}, actual={actualPayloadPaths.Length}.");

        foreach (string relativePath in actualPayloadPaths)
        {
            AssertTrue(
                manifestFiles.TryGetValue(relativePath, out ManifestEntry expected),
                $"Payload file is not listed in the manifest: {relativePath}");

            string fullPath = Path.Combine(releaseDirectory, relativePath.Replace('/', '\\'));
            FileInfo file = new(fullPath);
            AssertTrue(file.Length == expected.Length, $"Payload length mismatch: {relativePath}");

            using FileStream stream = File.OpenRead(fullPath);
            string actualHash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
            AssertEqual(expected.Sha256.ToLowerInvariant(), actualHash, $"payload SHA256 for {relativePath}");
        }

        VerifyResolvedPackageInventory(
            Path.Combine(repoRoot, "obj", "project.assets.json"),
            Path.Combine(releaseDirectory, "THIRD-PARTY-NOTICES.txt"));
    }

    private static void VerifyResolvedPackageInventory(string assetsPath, string noticePath)
    {
        AssertTrue(File.Exists(assetsPath), $"NuGet assets file is missing: {assetsPath}");
        string notices = File.ReadAllText(noticePath);
        using JsonDocument assetsDocument = JsonDocument.Parse(File.ReadAllText(assetsPath));
        foreach (JsonProperty library in assetsDocument.RootElement.GetProperty("libraries").EnumerateObject())
        {
            string type = library.Value.GetProperty("type").GetString() ?? string.Empty;
            if (!string.Equals(type, "package", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            int separator = library.Name.LastIndexOf('/');
            AssertTrue(separator > 0, $"Unexpected resolved package identity: {library.Name}");
            string packageIdentity = $"{library.Name[..separator]} {library.Name[(separator + 1)..]}";
            AssertTrue(
                notices.Contains(packageIdentity, StringComparison.OrdinalIgnoreCase),
                $"Third-party notices do not list the resolved package: {packageIdentity}");
        }
    }

    private static ProcessResult RunVerifier(string repoRoot)
    {
        string scriptPath = Path.Combine(repoRoot, "scripts", "publish-win-x64.ps1");
        ProcessStartInfo startInfo = new()
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = repoRoot
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);
        startInfo.ArgumentList.Add("-Configuration");
        startInfo.ArgumentList.Add("Release");
        startInfo.ArgumentList.Add("-Runtime");
        startInfo.ArgumentList.Add("win-x64");
        startInfo.ArgumentList.Add("-ReleaseVersion");
        startInfo.ArgumentList.Add(ExpectedVersion);
        startInfo.ArgumentList.Add("-VerifyOnly");

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Unable to start the release verifier.");
        string standardOutput = process.StandardOutput.ReadToEnd();
        string standardError = process.StandardError.ReadToEnd();
        if (!process.WaitForExit(120_000))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("Release verifier did not finish within 120 seconds.");
        }

        return new ProcessResult(process.ExitCode, standardOutput + standardError);
    }

    private static string FindRepositoryRoot()
    {
        foreach (string startPath in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory })
        {
            DirectoryInfo directory = new(startPath);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "OpenVisionLab.LabelingStudio.csproj")) &&
                    File.Exists(Path.Combine(directory.FullName, "global.json")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }
        }

        throw new DirectoryNotFoundException("Unable to find the labeling application repository root.");
    }

    private static void AssertEqual(string expected, string actual, string description)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Unexpected {description}. Expected '{expected}', actual '{actual}'.");
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private readonly record struct ManifestEntry(long Length, string Sha256);

    private readonly record struct ProcessResult(int ExitCode, string Output);
}
