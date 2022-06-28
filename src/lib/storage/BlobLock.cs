using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

namespace SunshineExpress.Storage.Blob;

public class BlobLock : IAsyncDisposable
{
    internal readonly BlobLeaseClient? blobLeaseClient;

    public string? LeaseId { get; private set; }

    private BlobLock(BlobLeaseClient? blobLeaseClient, string? leaseId)
    {
        this.blobLeaseClient = blobLeaseClient;
        LeaseId = leaseId;
    }

    public static async Task<BlobLock> AcquireAsync(BlobClient? blobClient, ETag? versionTag, TimeSpan duration)
    {
        if (blobClient is null || versionTag is null || !await blobClient.ExistsAsync())
            return new BlobLock(blobLeaseClient: null, leaseId: null);

        var blobLeaseClient = blobClient.GetBlobLeaseClient();
        var startTime = DateTimeOffset.Now;
        var waitUntil = DateTimeOffset.Now + TimeSpan.FromSeconds(60);

        BlobLease? lease = null;
        do
        {
            try
            {
                lease = await blobLeaseClient.AcquireAsync(duration);
                break;
            }
            catch
            {
                await Task.Delay(1000);
            }
        } while (waitUntil > startTime);

        if (lease is null)
            throw new Exception("Failed acquiring lease lock");

        if (lease.ETag != versionTag)
        {
            await blobLeaseClient.ReleaseAsync();
            throw new Exception("Acquired lock ETag doesn't match the current entity ETag");
        }

        return new BlobLock(blobLeaseClient, lease.LeaseId);
    }

    public async ValueTask DisposeAsync()
    {
        if (blobLeaseClient is not null && LeaseId is not null)
            await blobLeaseClient.ReleaseAsync();

        LeaseId = null;
    }
}
