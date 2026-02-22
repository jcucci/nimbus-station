using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace NimbusStation.Providers.Azure.Cli;

/// <summary>
/// Executes Azure CLI commands by spawning the az process.
/// </summary>
public sealed class AzureCliExecutor : IAzureCliExecutor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<AzureCliExecutor> _logger;
    private readonly string _azCommand;
    private bool? _isInstalled;
    private string? _cachedVersion;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureCliExecutor"/> class.
    /// </summary>
    public AzureCliExecutor(ILogger<AzureCliExecutor> logger)
    {
        _logger = logger;
        _azCommand = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "az.cmd" : "az";
    }

    /// <inheritdoc/>
    public async Task<AzureCliResult> ExecuteAsync(
        string arguments,
        int timeoutMs = 30000,
        CancellationToken cancellationToken = default)
    {
        if (!await IsInstalledAsync())
            return AzureCliResult.NotInstalled();

        var psi = new ProcessStartInfo
        {
            FileName = _azCommand,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        try
        {
            using var process = new Process { StartInfo = psi };
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            var stdoutLock = new object();
            var stderrLock = new object();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                {
                    lock (stdoutLock)
                        stdout.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                {
                    lock (stderrLock)
                        stderr.AppendLine(e.Data);
                }
            };

            _logger.LogDebug("Executing: az {Arguments}", arguments);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var timeoutCts = new CancellationTokenSource(timeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                await process.WaitForExitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                TryKillProcess(process);
                return AzureCliResult.Failed($"Command timed out after {timeoutMs}ms");
            }
            catch (OperationCanceledException)
            {
                TryKillProcess(process);
                throw;
            }

            var stdoutStr = stdout.ToString().TrimEnd();
            var stderrStr = stderr.ToString().TrimEnd();

            if (process.ExitCode == 0)
                return AzureCliResult.Succeeded(stdoutStr, stderrStr, process.ExitCode);

            var errorMessage = !string.IsNullOrWhiteSpace(stderrStr)
                ? stderrStr
                : $"Command failed with exit code {process.ExitCode}";

            return AzureCliResult.Failed(errorMessage, stderrStr, process.ExitCode);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return AzureCliResult.Failed($"Failed to execute Azure CLI: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<AzureCliResult<T>> ExecuteJsonAsync<T>(
        string arguments,
        int timeoutMs = 30000,
        CancellationToken cancellationToken = default)
    {
        var argumentsWithJson = arguments.Contains("--output")
            ? arguments
            : $"{arguments} --output json";

        var result = await ExecuteAsync(argumentsWithJson, timeoutMs, cancellationToken);

        if (!result.Success)
            return AzureCliResult<T>.Failed(result.ErrorMessage ?? "Command failed", result);

        if (string.IsNullOrWhiteSpace(result.StandardOutput))
            return AzureCliResult<T>.Failed("Command returned empty output", result);

        try
        {
            var data = JsonSerializer.Deserialize<T>(result.StandardOutput, JsonOptions);
            if (data is null)
                return AzureCliResult<T>.Failed("Failed to parse JSON output: result was null", result);

            return AzureCliResult<T>.Succeeded(data, result);
        }
        catch (JsonException ex)
        {
            return AzureCliResult<T>.Failed($"Failed to parse JSON output: {ex.Message}", result);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsInstalledAsync()
    {
        if (_isInstalled.HasValue)
            return _isInstalled.Value;

        _isInstalled = await CheckIsInstalledAsync();
        return _isInstalled.Value;
    }

    /// <inheritdoc/>
    public async Task<string?> GetVersionAsync()
    {
        if (_cachedVersion is not null)
            return _cachedVersion;

        if (!await IsInstalledAsync())
            return null;

        var result = await ExecuteVersionCommandAsync();
        if (!result.Success)
            return null;

        var firstLine = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (firstLine is not null && firstLine.StartsWith("azure-cli"))
        {
            _cachedVersion = firstLine.Trim();
            return _cachedVersion;
        }

        _cachedVersion = result.StandardOutput.Split('\n').FirstOrDefault()?.Trim();
        return _cachedVersion;
    }

    private async Task<bool> CheckIsInstalledAsync()
    {
        var result = await ExecuteVersionCommandAsync();
        return result.Success;
    }

    /// <summary>
    /// Executes 'az --version' with timeout and proper stream handling.
    /// Used by both IsInstalledAsync and GetVersionAsync.
    /// </summary>
    private async Task<AzureCliResult> ExecuteVersionCommandAsync()
    {
        var psi = new ProcessStartInfo
        {
            FileName = _azCommand,
            Arguments = "--version",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        try
        {
            using var process = new Process { StartInfo = psi };
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            var stdoutLock = new object();
            var stderrLock = new object();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                {
                    lock (stdoutLock)
                        stdout.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                {
                    lock (stderrLock)
                        stderr.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                await process.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                TryKillProcess(process);
                return AzureCliResult.Failed("Version check timed out");
            }

            var stdoutStr = stdout.ToString().TrimEnd();
            var stderrStr = stderr.ToString().TrimEnd();

            return process.ExitCode == 0
                ? AzureCliResult.Succeeded(stdoutStr, stderrStr, process.ExitCode)
                : AzureCliResult.Failed($"Version check failed with exit code {process.ExitCode}", stderrStr, process.ExitCode);
        }
        catch (Exception ex)
        {
            return AzureCliResult.Failed($"Failed to check Azure CLI: {ex.Message}");
        }
    }

    private static void TryKillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Ignore errors when killing process
        }
    }
}
