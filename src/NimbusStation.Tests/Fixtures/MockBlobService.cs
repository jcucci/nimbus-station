using NimbusStation.Providers.Azure.Blob;

namespace NimbusStation.Tests.Fixtures;

/// <summary>
/// Mock implementation of <see cref="IBlobService"/> for testing.
/// </summary>
public sealed class MockBlobService : IBlobService
{
    private ContainerListResult? _nextContainerResult;
    private BlobListResult? _nextBlobListResult;
    private BlobContentResult? _nextContentResult;
    private string? _nextDownloadPath;
    private Exception? _nextException;

    private readonly List<string> _listContainersCalls = [];
    private readonly List<(string AliasName, string? Prefix, int? MaxResults)> _listBlobsCalls = [];
    private readonly List<(string AliasName, string BlobName)> _getContentCalls = [];
    private readonly List<(string AliasName, string BlobName, string DestDir)> _downloadCalls = [];

    public IReadOnlyList<string> ListContainersCalls => _listContainersCalls;
    public IReadOnlyList<(string AliasName, string? Prefix, int? MaxResults)> ListBlobsCalls => _listBlobsCalls;
    public IReadOnlyList<(string AliasName, string BlobName)> GetContentCalls => _getContentCalls;
    public IReadOnlyList<(string AliasName, string BlobName, string DestDir)> DownloadCalls => _downloadCalls;

    public void SetupContainerListResult(ContainerListResult result)
    {
        _nextContainerResult = result;
        _nextException = null;
    }

    public void SetupBlobListResult(BlobListResult result)
    {
        _nextBlobListResult = result;
        _nextException = null;
    }

    public void SetupContentResult(BlobContentResult result)
    {
        _nextContentResult = result;
        _nextException = null;
    }

    public void SetupDownloadPath(string path)
    {
        _nextDownloadPath = path;
        _nextException = null;
    }

    public void SetupException(Exception exception)
    {
        _nextException = exception;
    }

    public static ContainerListResult CreateEmptyContainerResult() => new([]);

    public static ContainerListResult CreateContainerResult(params string[] names) =>
        new(names.Select(n => new ContainerInfo(n, DateTimeOffset.UtcNow)).ToList());

    public static BlobListResult CreateEmptyBlobResult() => new([]);

    public static BlobListResult CreateBlobResult(params (string Name, long Size)[] blobs) =>
        new(blobs.Select(b => new BlobInfo(b.Name, b.Size, DateTimeOffset.UtcNow, "application/octet-stream")).ToList());

    public static BlobContentResult CreateTextContent(string text) =>
        new(System.Text.Encoding.UTF8.GetBytes(text), "text/plain", IsBinary: false);

    public static BlobContentResult CreateBinaryContent(byte[] content, string contentType = "application/octet-stream") =>
        new(content, contentType, IsBinary: true);

    public Task<ContainerListResult> ListContainersAsync(string storageAliasName, CancellationToken cancellationToken = default)
    {
        _listContainersCalls.Add(storageAliasName);
        if (_nextException is not null) throw _nextException;
        return Task.FromResult(_nextContainerResult ?? CreateEmptyContainerResult());
    }

    public Task<BlobListResult> ListBlobsAsync(string blobAliasName, string? prefix = null, int? maxResults = null, CancellationToken cancellationToken = default)
    {
        _listBlobsCalls.Add((blobAliasName, prefix, maxResults));
        if (_nextException is not null) throw _nextException;
        return Task.FromResult(_nextBlobListResult ?? CreateEmptyBlobResult());
    }

    public Task<BlobContentResult> GetBlobContentAsync(string blobAliasName, string blobName, CancellationToken cancellationToken = default)
    {
        _getContentCalls.Add((blobAliasName, blobName));
        if (_nextException is not null) throw _nextException;
        return Task.FromResult(_nextContentResult ?? CreateTextContent(""));
    }

    public Task<string> DownloadBlobAsync(string blobAliasName, string blobName, string destinationDirectory, CancellationToken cancellationToken = default)
    {
        _downloadCalls.Add((blobAliasName, blobName, destinationDirectory));
        if (_nextException is not null) throw _nextException;
        return Task.FromResult(_nextDownloadPath ?? Path.Combine(destinationDirectory, blobName));
    }
}
