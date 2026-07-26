using MvcVisionSystem._3._Communication.TCP;
using MvcVisionSystem.Yolo;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace LabelingApplication.Tests;

internal static class TestSupport
{
    internal static string ComputeFileSha256(string path)
        => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    internal static string CaptureExternalSourceSnapshot(string sourceRoot)
    {
        return string.Join(
            "\n",
            Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Select(path => Path.GetRelativePath(sourceRoot, path).Replace('\\', '/') + ":" + ComputeFileSha256(path)));
    }

    internal static ExternalYoloSourceTreeSnapshot CaptureExternalYoloSourceTree(string sourceRoot)
    {
        string root = Path.GetFullPath(sourceRoot);
        string[] filePaths = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var manifestLines = new List<string>(filePaths.Length);
        var cacheRelativePaths = new List<string>();
        using SHA256 treeHash = SHA256.Create();
        foreach (string filePath in filePaths)
        {
            string relativePath = Path.GetRelativePath(root, filePath).Replace('\\', '/');
            string fileHash = ComputeFileSha256(filePath);
            string manifestLine = relativePath + "\t" + fileHash;
            manifestLines.Add(manifestLine);
            if (relativePath.EndsWith(".cache", StringComparison.OrdinalIgnoreCase))
            {
                cacheRelativePaths.Add(relativePath);
            }

            byte[] entry = Encoding.UTF8.GetBytes(manifestLine + "\n");
            treeHash.TransformBlock(entry, 0, entry.Length, entry, 0);
        }

        treeHash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        string[] temporaryDirectoryRelativePaths = Directory.EnumerateDirectories(root, "openvisionlab-*", SearchOption.AllDirectories)
            .Where(path => Path.GetFileName(path).StartsWith("openvisionlab-yolov5-training-", StringComparison.OrdinalIgnoreCase)
                || Path.GetFileName(path).StartsWith("openvisionlab-ultralytics-label-cache-", StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new ExternalYoloSourceTreeSnapshot(
            filePaths.Length,
            BitConverter.ToString(treeHash.Hash ?? Array.Empty<byte>()).Replace("-", string.Empty),
            manifestLines,
            cacheRelativePaths,
            temporaryDirectoryRelativePaths);
    }

    internal static string CreateTempRoot()
    {
        string root = Path.Combine(Path.GetTempPath(), "LabelingApplication.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    internal static void DeleteTempRoot(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    internal static void DeleteDirectoryIfExists(string path)
    {
        if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    internal static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    internal static void AssertEqual<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
        }
    }

    internal static Bitmap CreateSolidBitmap(int width, int height, Color color)
    {
        var bitmap = new Bitmap(width, height);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(color);
        return bitmap;
    }

    internal static Size GetImageSize(string imagePath)
    {
        using Bitmap bitmap = new Bitmap(imagePath);
        return bitmap.Size;
    }

    internal static int CountSavedMaskPixels(string maskPath)
    {
        using Bitmap bitmap = new Bitmap(maskPath);
        int nonZeroPixels = 0;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                Color pixel = bitmap.GetPixel(x, y);
                if (pixel.R != 0 || pixel.G != 0 || pixel.B != 0)
                {
                    nonZeroPixels++;
                }
            }
        }

        return nonZeroPixels;
    }

    internal static Process StartRealYoloTrainingClient(
        string pythonPath,
        string clientScriptPath,
        string modelRoot,
        string imageRoot,
        string weightsPath,
        int port,
        int imageSize,
        StringBuilder stdout,
        StringBuilder stderr,
        string modelName = "",
        string device = "cpu")
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = pythonPath,
            WorkingDirectory = modelRoot,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        startInfo.ArgumentList.Add(clientScriptPath);
        startInfo.ArgumentList.Add("--host");
        startInfo.ArgumentList.Add("127.0.0.1");
        startInfo.ArgumentList.Add("--port");
        startInfo.ArgumentList.Add(port.ToString(CultureInfo.InvariantCulture));
        startInfo.ArgumentList.Add("--timeout");
        startInfo.ArgumentList.Add("60");
        startInfo.ArgumentList.Add("--retry");
        startInfo.ArgumentList.Add("--retry-delay");
        startInfo.ArgumentList.Add("1");
        startInfo.ArgumentList.Add("--weights");
        startInfo.ArgumentList.Add(weightsPath);
        startInfo.ArgumentList.Add("--model-root");
        startInfo.ArgumentList.Add(modelRoot);
        startInfo.ArgumentList.Add("--image-root");
        startInfo.ArgumentList.Add(imageRoot);
        startInfo.ArgumentList.Add("--device");
        startInfo.ArgumentList.Add(string.IsNullOrWhiteSpace(device) ? "cpu" : device);
        startInfo.ArgumentList.Add("--img-size");
        startInfo.ArgumentList.Add(imageSize.ToString(CultureInfo.InvariantCulture));
        startInfo.ArgumentList.Add("--conf");
        startInfo.ArgumentList.Add("0");
        if (!string.IsNullOrWhiteSpace(modelName))
        {
            startInfo.ArgumentList.Add("--model");
            startInfo.ArgumentList.Add(modelName);
        }

        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };
        process.OutputDataReceived += (_, e) => AppendProcessLine(stdout, e.Data);
        process.ErrorDataReceived += (_, e) => AppendProcessLine(stderr, e.Data);

        AssertTrue(process.Start(), "YOLO training Python client did not start");
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    internal static void StopRealYoloClient(Process process)
    {
        if (process == null)
        {
            return;
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
            }
        }
        finally
        {
            process.Dispose();
        }
    }

    internal static void WriteRealYoloProcessLog(string artifactRoot, StringBuilder stdout, StringBuilder stderr)
    {
        if (string.IsNullOrWhiteSpace(artifactRoot))
        {
            return;
        }

        Directory.CreateDirectory(artifactRoot);
        File.WriteAllText(Path.Combine(artifactRoot, "python-stdout.txt"), Snapshot(stdout));
        File.WriteAllText(Path.Combine(artifactRoot, "python-stderr.txt"), Snapshot(stderr));
    }

    internal static string BuildRealYoloSmokeFailure(string message, StringBuilder stdout, StringBuilder stderr)
    {
        string stderrText = TrimForMessage(Snapshot(stderr), 1600);
        string stdoutText = TrimForMessage(Snapshot(stdout), 1600);
        return $"{message}. stderr: {stderrText} stdout: {stdoutText}";
    }

    internal static void AppendProcessLine(StringBuilder builder, string line)
    {
        if (builder == null || line == null)
        {
            return;
        }

        lock (builder)
        {
            builder.AppendLine(line);
        }
    }

    private static string Snapshot(StringBuilder builder)
    {
        if (builder == null)
        {
            return string.Empty;
        }

        lock (builder)
        {
            return builder.ToString();
        }
    }

    private static string TrimForMessage(string text, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "(empty)";
        }

        text = text.Trim();
        return text.Length <= maximumLength
            ? text
            : text.Substring(text.Length - maximumLength);
    }

    internal static string FindRepositoryRoot()
    {
        DirectoryInfo directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "OpenVisionLab.LabelingStudio.csproj")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }

    internal static string GetArgumentValue(string[] args, string name, string defaultValue)
    {
        string prefix = name + "=";
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return arg.Substring(prefix.Length).Trim('"');
            }

            if (string.Equals(arg, name, StringComparison.OrdinalIgnoreCase)
                && i + 1 < args.Length
                && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                return args[i + 1].Trim('"');
            }
        }

        return defaultValue;
    }

    internal static int GetPositiveArgument(string[] args, string name, int fallback)
    {
        string text = GetArgumentValue(args, name, fallback.ToString(CultureInfo.InvariantCulture));
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) && value > 0
            ? value
            : fallback;
    }

    internal static string ReadWpfLabelingShellWindowSources()
    {
        string viewsRoot = Path.Combine(FindRepositoryRoot(), "0. UI", "9) WPF", "Views");
        return string.Join(
            Environment.NewLine,
            Directory.GetFiles(viewsRoot, "WpfLabelingShellWindow*.cs")
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Select(File.ReadAllText));
    }

    internal static string FindMethodSourceBlock(string source, string signature)
    {
        int signatureIndex = source?.IndexOf(signature, StringComparison.Ordinal) ?? -1;
        if (signatureIndex < 0)
        {
            return string.Empty;
        }

        int bodyStart = source.IndexOf('{', signatureIndex);
        if (bodyStart < 0)
        {
            return string.Empty;
        }

        int depth = 0;
        for (int i = bodyStart; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source.Substring(signatureIndex, i - signatureIndex + 1);
                }
            }
        }

        return string.Empty;
    }

    internal static bool ContainsVisibleMojibakeArtifact(string text)
    {
        return !string.IsNullOrEmpty(text)
            && text.Any(ch => ch == '\uFFFD' || (ch >= '\u4E00' && ch <= '\u9FFF') || (ch >= '\uF900' && ch <= '\uFAFF'));
    }

    internal static void AssertNamedXamlAttachedBinding(
        XDocument xaml,
        XName xName,
        string controlName,
        string attachedPropertySuffix,
        string expectedBindingProperty)
    {
        XElement element = xaml.Descendants()
            .FirstOrDefault(candidate => string.Equals((string)candidate.Attribute(xName), controlName, StringComparison.Ordinal));

        AssertTrue(element != null, $"WPF bound control was not found: {controlName}");
        string binding = (string)element.Attributes()
            .FirstOrDefault(attribute => attribute.Name.LocalName.EndsWith(attachedPropertySuffix, StringComparison.Ordinal)) ?? string.Empty;
        AssertTrue(
            binding.Contains($"Binding {expectedBindingProperty}", StringComparison.Ordinal),
            $"WPF control {controlName}.{attachedPropertySuffix} was not bound to {expectedBindingProperty}");
    }

    internal static void AssertNamedXamlElement(XDocument xaml, XName xName, string localName, string controlName)
    {
        XElement element = xaml.Descendants()
            .FirstOrDefault(candidate => candidate.Name.LocalName == localName
                && string.Equals((string)candidate.Attribute(xName), controlName, StringComparison.Ordinal));

        AssertTrue(element != null, $"WPF {localName} was not found: {controlName}");
    }

    internal static void AssertNamedXamlBinding(
        XDocument xaml,
        XName xName,
        string controlName,
        string targetPropertyName,
        string expectedBindingProperty)
    {
        XElement element = xaml.Descendants()
            .FirstOrDefault(candidate => string.Equals((string)candidate.Attribute(xName), controlName, StringComparison.Ordinal));

        AssertTrue(element != null, $"WPF bound control was not found: {controlName}");
        string binding = (string)element.Attribute(targetPropertyName) ?? string.Empty;
        AssertTrue(
            binding.Contains($"Binding {expectedBindingProperty}", StringComparison.Ordinal),
            $"WPF control {controlName}.{targetPropertyName} was not bound to {expectedBindingProperty}");
    }

    internal static void AssertNamedXamlValue(
        XDocument xaml,
        XName xName,
        string controlName,
        string propertyName,
        string expectedValue)
    {
        XElement element = xaml.Descendants()
            .FirstOrDefault(candidate => string.Equals((string)candidate.Attribute(xName), controlName, StringComparison.Ordinal));

        AssertTrue(element != null, $"WPF control was not found: {controlName}");
        XAttribute attribute = element.Attributes()
            .FirstOrDefault(candidate => string.Equals(candidate.Name.LocalName, propertyName, StringComparison.Ordinal)
                || string.Equals(candidate.Name.ToString(), propertyName, StringComparison.Ordinal)
                || candidate.Name.LocalName.EndsWith("." + propertyName, StringComparison.Ordinal)
                || candidate.Name.ToString().EndsWith("." + propertyName, StringComparison.Ordinal));
        AssertTrue(attribute != null, $"WPF control {controlName}.{propertyName} was not found");
        AssertEqual(expectedValue, (string)attribute);
    }

    internal static void InvokePrivate(object instance, string methodName)
    {
        MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        AssertTrue(method != null, $"private method was not found: {methodName}");
        try
        {
            method.Invoke(instance, null);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    internal static object GetRuntimeButtonCommand(object button)
    {
        if (button is System.Windows.Controls.Primitives.ButtonBase buttonBase && buttonBase.Command != null)
        {
            return buttonBase.Command;
        }

        return button?.GetType().GetProperty("Command")?.GetValue(button);
    }

    internal static void PumpWpfDispatcher(TimeSpan duration)
    {
        System.Windows.Threading.DispatcherFrame frame = new System.Windows.Threading.DispatcherFrame();
        System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer(
            System.Windows.Threading.DispatcherPriority.Background)
        {
            Interval = duration
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            frame.Continue = false;
        };
        timer.Start();
        System.Windows.Threading.Dispatcher.PushFrame(frame);
    }

    internal static T InvokePrivateResult<T>(object instance, string methodName, params object[] args)
    {
        MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        AssertTrue(method != null, $"private method was not found: {methodName}");
        try
        {
            object result = method.Invoke(instance, args);
            return result is T typed ? typed : default;
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    internal static T GetPrivateField<T>(object instance, string fieldName)
    {
        FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        AssertTrue(field != null, $"private field was not found: {fieldName}");
        object value = field.GetValue(instance);
        if (value == null)
        {
            return default;
        }

        AssertTrue(value is T, $"private field had unexpected type: {fieldName}");
        return (T)value;
    }

    internal static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        AssertTrue(field != null, $"private field was not found: {fieldName}");
        field.SetValue(instance, value);
    }

    internal static bool WaitUntilWpf(Func<bool> condition, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow <= deadline)
        {
            PumpWpfDispatcher(TimeSpan.FromMilliseconds(50));
            if (condition())
            {
                return true;
            }

            System.Threading.Thread.Sleep(10);
        }

        PumpWpfDispatcher(TimeSpan.FromMilliseconds(50));
        return condition();
    }

    internal static string GetEnvironmentValue(string name, string fallback)
    {
        string value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    internal static bool WaitUntil(Func<bool> condition, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow <= deadline)
        {
            if (condition())
            {
                return true;
            }

            Thread.Sleep(50);
        }

        return condition();
    }

    internal static int GetAvailableTcpPort()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    internal static T InvokePrivateStaticResult<T>(Type type, string methodName, params object[] args)
    {
        MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
        AssertTrue(method != null, $"private static method was not found: {methodName}");
        try
        {
            object result = method.Invoke(null, args);
            return result is T typed ? typed : default;
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    internal static void RunMockTrainingPacketCaptureClient(
        int port,
        ManualResetEventSlim requestReceived,
        Action<YoloTrainingRequest> inspectRequest)
    {
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, port);
        using NetworkStream stream = client.GetStream();
        stream.ReadTimeout = 30000;
        stream.WriteTimeout = 5000;

        string packet = ReadTrainingPacket(stream);
        AssertTrue(packet.StartsWith("StartTraining", StringComparison.Ordinal), "mock worker received an unexpected command");
        AssertTrainingPacketDoesNotApproveModelDownload(packet);
        string[] parts = packet.Split(LearningProtocol.PacketSeparator);
        AssertEqual(2, parts.Length);
        YoloTrainingRequest request = JsonConvert.DeserializeObject<YoloTrainingRequest>(parts[1]);
        AssertTrue(request != null, "StartTraining payload was not parsed");
        inspectRequest?.Invoke(request);
        requestReceived.Set();

        WriteJsonLine(stream, "{\"type\":\"TrainYoloResult\",\"version\":1,\"ok\":true,\"taskId\":\"task-smoke\",\"state\":\"started\"}");
    }

    internal static void WriteJsonLine(NetworkStream stream, string json)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(json + "\n");
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }

    internal static void AssertTrainingPacketDoesNotApproveModelDownload(string packet)
    {
        AssertTrue(!packet.Contains("allowModelDownload", StringComparison.Ordinal)
            && !packet.Contains("allowWeightDownload", StringComparison.Ordinal)
            && !packet.Contains("allowDownload", StringComparison.Ordinal),
            "StartTraining packet should not opt into implicit model downloads without an explicit UI approval path");
    }

    internal static string ReadTrainingPacket(NetworkStream stream)
    {
        var buffer = new List<byte>();
        byte[] chunk = new byte[2048];
        DateTime deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow <= deadline)
        {
            int read = stream.Read(chunk, 0, chunk.Length);
            if (read <= 0)
            {
                break;
            }

            buffer.AddRange(chunk.Take(read));
            string text = Encoding.UTF8.GetString(buffer.ToArray());
            if (text.Contains("StartTraining", StringComparison.Ordinal)
                && text.Contains(LearningProtocol.PacketSeparator, StringComparison.Ordinal)
                && text.Contains("dataYaml", StringComparison.Ordinal)
                && text.TrimEnd().EndsWith("}", StringComparison.Ordinal))
            {
                return text;
            }
        }

        throw new InvalidOperationException("mock worker timed out while reading StartTraining packet");
    }
}

internal sealed class ExternalYoloSourceTreeSnapshot
{
    internal ExternalYoloSourceTreeSnapshot(
        int fileCount,
        string treeSha256,
        IReadOnlyList<string> manifestLines,
        IReadOnlyList<string> cacheRelativePaths,
        IReadOnlyList<string> temporaryDirectoryRelativePaths)
    {
        FileCount = fileCount;
        TreeSha256 = treeSha256 ?? string.Empty;
        ManifestLines = manifestLines ?? Array.Empty<string>();
        CacheRelativePaths = cacheRelativePaths ?? Array.Empty<string>();
        TemporaryDirectoryRelativePaths = temporaryDirectoryRelativePaths ?? Array.Empty<string>();
    }

    internal int FileCount { get; }

    internal string TreeSha256 { get; }

    internal IReadOnlyList<string> ManifestLines { get; }

    internal IReadOnlyList<string> CacheRelativePaths { get; }

    internal IReadOnlyList<string> TemporaryDirectoryRelativePaths { get; }
}
