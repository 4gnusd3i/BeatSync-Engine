using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BeatSync.Desktop.Models;

namespace BeatSync.Desktop.Services;

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

    public Task<RuntimeStatusDto> ProbeRuntimeAsync(CancellationToken cancellationToken = default)
        => RunJsonCommandAsync<RuntimeStatusDto>("probe-runtime", cancellationToken: cancellationToken);

    public Task<SourceInspectionDto> InspectSourcesAsync(string audioPath, string videoFolder, CancellationToken cancellationToken = default)
        => RunJsonCommandAsync<SourceInspectionDto>(
            "inspect-sources",
            startInfo =>
            {
                startInfo.ArgumentList.Add("--audio-path");
                startInfo.ArgumentList.Add(audioPath ?? string.Empty);
                startInfo.ArgumentList.Add("--video-folder");
                startInfo.ArgumentList.Add(videoFolder ?? string.Empty);
            },
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

        return payload.RecommendedFilename;
    }

    public Task<RenderResultDto> RenderAsync(RenderRequestDto request, CancellationToken cancellationToken = default)
    {
        var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
        return RunJsonCommandAsync<RenderResultDto>(
            "render",
            standardInput: requestJson,
            cancellationToken: cancellationToken
        );
    }

    private async Task<T> RunJsonCommandAsync<T>(
        string command,
        Action<ProcessStartInfo>? configureStartInfo = null,
        string? standardInput = null,
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

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        if (standardInput is not null)
        {
            await process.StandardInput.WriteAsync(standardInput);
            await process.StandardInput.FlushAsync();
            process.StandardInput.Close();
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync(cancellationToken);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
        {
            var message = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
            throw new InvalidOperationException($"Bridge command '{command}' failed: {message.Trim()}");
        }

        if (string.IsNullOrWhiteSpace(stdout))
        {
            throw new InvalidOperationException($"Bridge command '{command}' returned no JSON payload.");
        }

        var payload = JsonSerializer.Deserialize<T>(stdout, _jsonOptions);
        if (payload is null)
        {
            throw new InvalidOperationException($"Bridge command '{command}' returned invalid JSON: {stdout}");
        }

        return payload;
    }

    private sealed class RecommendationResponse
    {
        [JsonPropertyName("recommended_filename")]
        public string RecommendedFilename { get; set; } = string.Empty;
    }
}
