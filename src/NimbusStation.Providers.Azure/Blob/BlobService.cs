using System.Runtime.InteropServices;
using System.Text.Json;
using NimbusStation.Infrastructure.Configuration;
using NimbusStation.Providers.Azure.Cli;

namespace NimbusStation.Providers.Azure.Blob;

/// <summary>
/// Service for interacting with Azure Blob Storage using the Azure CLI.
/// Uses --auth-mode login for Azure AD authentication.
/// </summary>
public sealed class BlobService : IBlobService
{
    private readonly IAzureCliExecutor _cliExecutor;
    private readonly IConfigurationService _configurationService;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobService"/> class.
    /// </summary>
    public BlobService(IAzureCliExecutor cliExecutor, IConfigurationService configurationService)
    {
        _cliExecutor = cliExecutor ?? throw new ArgumentNullException(nameof(cliExecutor));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
    }

    /// <inheritdoc/>
    public async Task<ContainerListResult> ListContainersAsync(
        string storageAliasName, CancellationToken cancellationToken = default)
    {
        var aliasConfig = _configurationService.GetStorageAlias(storageAliasName)
            ?? throw new InvalidOperationException($"Storage alias '{storageAliasName}' not found in config.");

        var arguments = $"storage container list --account-name {aliasConfig.Account} --auth-mode login";
        var result = await _cliExecutor.ExecuteJsonAsync<List<AzureContainerResponse>>(arguments, cancellationToken: cancellationToken);

        if (!result.Success)
            throw new InvalidOperationException(result.ErrorMessage ?? "Failed to list containers.");

        var containers = result.Data?
            .Select(c => new ContainerInfo(c.Name, ParseDateTimeOffset(c.LastModified)))
            .ToList() ?? [];

        return new ContainerListResult(containers);
    }

    /// <inheritdoc/>
    public async Task<BlobListResult> ListBlobsAsync(
        string blobAliasName, string? prefix = null, CancellationToken cancellationToken = default)
    {
        var aliasConfig = _configurationService.GetBlobAlias(blobAliasName)
            ?? throw new InvalidOperationException($"Blob alias '{blobAliasName}' not found in config.");

        var arguments = $"storage blob list --account-name {aliasConfig.Account} --container-name {aliasConfig.Container} --auth-mode login";
        if (!string.IsNullOrWhiteSpace(prefix))
            arguments += $" --prefix \"{prefix}\"";

        var result = await _cliExecutor.ExecuteJsonAsync<List<AzureBlobResponse>>(arguments, cancellationToken: cancellationToken);

        if (!result.Success)
            throw new InvalidOperationException(result.ErrorMessage ?? "Failed to list blobs.");

        var blobs = result.Data?
            .Select(b => new BlobInfo(
                b.Name,
                b.Properties?.ContentLength ?? 0,
                ParseDateTimeOffset(b.Properties?.LastModified),
                b.Properties?.ContentType ?? "application/octet-stream"))
            .ToList() ?? [];

        return new BlobListResult(blobs);
    }

    /// <inheritdoc/>
    public async Task<BlobContentResult> GetBlobContentAsync(
        string blobAliasName, string blobName, CancellationToken cancellationToken = default)
    {
        var aliasConfig = _configurationService.GetBlobAlias(blobAliasName)
            ?? throw new InvalidOperationException($"Blob alias '{blobAliasName}' not found in config.");

        // First, get blob properties to determine content type
        var propsArguments = $"storage blob show --account-name {aliasConfig.Account} --container-name {aliasConfig.Container} --name \"{blobName}\" --auth-mode login";
        var propsResult = await _cliExecutor.ExecuteJsonAsync<AzureBlobResponse>(propsArguments, cancellationToken: cancellationToken);

        if (!propsResult.Success)
            throw new InvalidOperationException(propsResult.ErrorMessage ?? $"Failed to get blob properties for '{blobName}'.");

        var contentType = propsResult.Data?.Properties?.ContentType ?? "application/octet-stream";
        var isBinary = BlobContentResult.IsBinaryContentType(contentType);

        // Download to a temp file and read content
        var tempFile = Path.GetTempFileName();
        try
        {
            var downloadArgs = $"storage blob download --account-name {aliasConfig.Account} --container-name {aliasConfig.Container} --name \"{blobName}\" --file \"{tempFile}\" --auth-mode login";
            var downloadResult = await _cliExecutor.ExecuteAsync(downloadArgs, cancellationToken: cancellationToken);

            if (!downloadResult.Success)
                throw new InvalidOperationException(downloadResult.ErrorMessage ?? $"Failed to download blob '{blobName}'.");

            var content = await File.ReadAllBytesAsync(tempFile, cancellationToken);
            return new BlobContentResult(content, contentType, isBinary);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    /// <inheritdoc/>
    public async Task<string> DownloadBlobAsync(
        string blobAliasName, string blobName, string destinationDirectory, CancellationToken cancellationToken = default)
    {
        var aliasConfig = _configurationService.GetBlobAlias(blobAliasName)
            ?? throw new InvalidOperationException($"Blob alias '{blobAliasName}' not found in config.");

        // Preserve blob path structure in destination
        var destinationPath = Path.Combine(destinationDirectory, blobName.Replace('/', Path.DirectorySeparatorChar));
        var destinationDir = Path.GetDirectoryName(destinationPath);

        if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
            Directory.CreateDirectory(destinationDir);

        var arguments = $"storage blob download --account-name {aliasConfig.Account} --container-name {aliasConfig.Container} --name \"{blobName}\" --file \"{destinationPath}\" --auth-mode login";
        var result = await _cliExecutor.ExecuteAsync(arguments, cancellationToken: cancellationToken);

        if (!result.Success)
            throw new InvalidOperationException(result.ErrorMessage ?? $"Failed to download blob '{blobName}'.");

        return destinationPath;
    }

    private static DateTimeOffset ParseDateTimeOffset(string? value) =>
        DateTimeOffset.TryParse(value, out var result) ? result : DateTimeOffset.MinValue;

    // Internal classes for deserializing Azure CLI JSON responses
    private sealed class AzureContainerResponse
    {
        public string Name { get; set; } = string.Empty;
        public string? LastModified { get; set; }
    }

    private sealed class AzureBlobResponse
    {
        public string Name { get; set; } = string.Empty;
        public AzureBlobProperties? Properties { get; set; }
    }

    private sealed class AzureBlobProperties
    {
        public long ContentLength { get; set; }
        public string? ContentType { get; set; }
        public string? LastModified { get; set; }
    }
}
