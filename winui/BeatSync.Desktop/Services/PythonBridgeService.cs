using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BeatSync.Desktop.Models;

namespace BeatSync.Desktop.Services;

public sealed record BridgeCommandResult<T>(T Payload, string StandardError);

public sealed class BridgeCommandException : InvalidOperationException
{
    public BridgeCommandException(string message, string standardError)
        : base(message)
    {
        StandardError = standardError;
    }

    public string StandardError { get; }
}

public sealed class PythonBridgeService
{
    private readonly string _repoRoot;
    private readonly string _pythonExe;
    private readonly string _bridgeScript;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public PythonBridgeService(string repoRoot)
    {
        _repoRoot = repoRoot;
        _pythonExe = Path.Combine(repoRoot, "bin", "python-3.13.9-embed-amd64", "python.exe");
        _bridgeScript = Path.Combine(repoRoot, "beatsync_bridge.py");
    }

    public Task<BridgeCommandResult<RuntimeStatusDto>> ProbeRuntimeAsync(
        Action<string>? onStandardErrorLine = null,
        CancellationToken cancellationToken = default)
        => RunJsonCommandAsync<RuntimeStatusDto>(
            "probe-runtime",
            onStandardErrorLine: onStandardErrorLine,
            cancellationToken: cancellationToken);

    public Task<BridgeCommandResult<SourceInspectionDto>> InspectSourcesAsync(
        string audioPath,
        string videoFolder,
        Action<string>? onStandardErrorLine = null,
        CancellationToken cancellationToken = default)
        => RunJsonCommandAsync<SourceInspectionDto>(
            "inspect-sources",
            startInfo =>
            {
                startInfo.ArgumentList.Add("--audio-path");
                startInfo.ArgumentList.Add(audioPath ?? string.Empty);
                startInfo.ArgumentList.Add("--video-folder");
                startInfo.ArgumentList.Add(videoFolder ?? string.Empty);
            },
            onStandardErrorLine: onStandardErrorLine,
            cancellationToken: cancellationToken
        );

    public async Task<string> RecommendOutputFilenameAsync(string currentFilename, string processingMode, CancellationToken cancellationToken = default)
    {
        var payload = await RunJsonCommandAsync<RecommendationResponse>(
            "recommend-output-name",
            startInfo =>
            {
                startInfo.ArgumentList.Add("--current-filename");
                startInfo.ArgumentList.Add(currentFilename ?? string.Empty);
                startInfo.ArgumentList.Add("--processing-mode");
                startInfo.ArgumentList.Add(processingMode);
            },
            cancellationToken: cancellationToken
        );

        return payload.Payload.RecommendedFilename;
    }

    public Task<BridgeCommandResult<RenderResultDto>> RenderAsync(
        RenderRequestDto request,
        Action<string>? onStandardErrorLine = null,
        CancellationToken cancellationToken = default)
    {
        var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
        return RunJsonCommandAsync<RenderResultDto>(
            "render",
            standardInput: requestJson,
            onStandardErrorLine: onStandardErrorLine,
            cancellationToken: cancellationToken
        );
    }

    private async Task<BridgeCommandResult<T>> RunJsonCommandAsync<T>(
        string command,
        Action<ProcessStartInfo>? configureStartInfo = null,
        string? standardInput = null,
        Action<string>? onStandardErrorLine = null,
        CancellationToken cancellationToken = default
    )
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _pythonExe,
            WorkingDirectory = _repoRoot,
            RedirectStandardInput = standardInput is not null,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        startInfo.ArgumentList.Add(_bridgeScript);
        startInfo.ArgumentList.Add(command);
        configureStartInfo?.Invoke(startInfo);

        var stderrBuilder = new StringBuilder();
        var stderrCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true,
        };

        process.ErrorDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data is null)
            {
                stderrCompleted.TrySetResult(true);
                return;
            }

            if (stderrBuilder.Length > 0)
            {
                stderrBuilder.AppendLine();
            }

            stderrBuilder.Append(eventArgs.Data);
            onStandardErrorLine?.Invoke(eventArgs.Data);
        };

        process.Start();
        process.BeginErrorReadLine();

        if (standardInput is not null)
        {
            await process.StandardInput.WriteAsync(standardInput);
            await process.StandardInput.FlushAsync();
            process.StandardInput.Close();
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync();

        await process.WaitForExitAsync(cancellationToken);
        await stderrCompleted.Task.WaitAsync(cancellationToken);

        var stdout = await stdoutTask;
        var stderr = stderrBuilder.ToString();

        if (process.ExitCode != 0)
        {
            var diagnosticLog = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
            throw new BridgeCommandException(
                BuildFailureMessage(command, stdout, stderr),
                diagnosticLog);
        }

        if (string.IsNullOrWhiteSpace(stdout))
        {
            throw new BridgeCommandException(
                $"Bridge command '{command}' returned no JSON payload.",
                stderr);
        }

        var payload = JsonSerializer.Deserialize<T>(stdout, _jsonOptions);
        if (payload is null)
        {
            throw new BridgeCommandException(
                $"Bridge command '{command}' returned invalid JSON.",
                string.IsNullOrWhiteSpace(stderr) ? stdout : stderr);
        }

        return new BridgeCommandResult<T>(payload, stderr);
    }

    private static string BuildFailureMessage(string command, string stdout, string stderr)
    {
        var detailSource = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
        var summary = detailSource
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault();

        return string.IsNullOrWhiteSpace(summary)
            ? $"Bridge command '{command}' failed."
            : $"Bridge command '{command}' failed: {summary.Trim()}";
    }

    private sealed class RecommendationResponse
    {
        [JsonPropertyName("recommended_filename")]
        public string RecommendedFilename { get; set; } = string.Empty;
    }
}
