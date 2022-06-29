using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using SunshineExpress.Service.Contract;
using SunshineExpress.Service.Contract.Storage;
using SunshineExpress.Storage.Blob.Configuration;
using SunshineExpress.Storage.Blob.Util;
using System.Text;
using System.Text.Json;

namespace SunshineExpress.Storage.Blob;

public class BlobStorage : IStorageClient
{
    private readonly BlobServiceClient serviceClient;
    private BlobContainerClient? containerClient;
    private static readonly SemaphoreSlim syncRoot = new(1);
    private readonly string containerName;
    private readonly ILogger logger;

    public BlobStorage(BlobStorageConfiguration configuration, ILogger<BlobStorage> logger)
    {
        serviceClient = new BlobServiceClient(configuration.ConnectionString);
        containerName = configuration.ContainerName;
        this.logger = logger;
    }

    /// <inheritdoc />
    public IEntityId<TEntity> CreateEntityId<TEntity>(string entityKey) where TEntity : IEntity<TEntity>, new()
        => EntityId<TEntity>.Create(entityKey);

    /// <inheritdoc />
    public virtual async Task<TEntity> Get<TEntity>(IEntityId<TEntity> entityId)
         where TEntity : IEntity<TEntity>, new()
    {
        var internalEntityId = (EntityId<TEntity>)entityId;
        var name = entityId.ToString();
        var client = await GetOrCreateContainerClient().ConfigureAwait(false);
        var blobClient = client.GetBlobClient(name);
        if (!await blobClient.ExistsAsync().ConfigureAwait(false))
        {
            logger.LogDebug($"Blob {containerName}/{name} does not exist.");
            var result = new TEntity();
            result.SetEntityId(entityId);
            internalEntityId.SetBlobClient(blobClient);

            return result;
        }

        var properties = (await blobClient.GetPropertiesAsync()).Value;
        using var contentStream = new MemoryStream();
        logger.LogDebug($"Downloading the blob {typeof(TEntity).Name}/{name}.");
        var blob = await blobClient.DownloadToAsync(contentStream, conditions: FixedETag(properties.ETag)).ConfigureAwait(false);
        logger.LogDebug($"Blob {containerName}/{name} (size: {contentStream.Length}) downloaded successfully.");

        var entity = JsonSerializer.Deserialize<TEntity>(Encoding.UTF8.GetString(contentStream.ToArray()));
        if (entity == null)
            throw new Exception($"Deserialization of entity {containerName}/{name} failed.");

        entity.SetEntityId(entityId);
        internalEntityId.SetBlobClient(blobClient);
        internalEntityId.VersionTag = properties.ETag;

        return entity;
    }

    /// <inheritdoc />
    public virtual async Task Add<TEntity>(TEntity entity)
        where TEntity : IEntity<TEntity>, new()
    {
        logger.LogDebug($"Adding a new entity {entity.EntityId}");
        var internalEntityId = (EntityId<TEntity>)entity.EntityId;
        if (internalEntityId.Exists)
            throw new InvalidOperationException("Cannot 'Add' existing entity, use 'Update' instead");

        if (entity.EntityId is null)
            throw new InvalidOperationException("Entity must have its EntityId set before saving it");

        var name = entity.EntityId.ToString();
        var client = await GetOrCreateContainerClient().ConfigureAwait(false);
        var blobClient = client.GetBlobClient(name);
        internalEntityId.SetBlobClient(blobClient);

        var contents = JsonSerializer.Serialize(entity);
        logger.LogDebug($"Uploading entity {entity.EntityId} to the storage.");
        var newVersion = await UploadBlobAsync(new MemoryStream(Encoding.UTF8.GetBytes(contents)), versionTag: null, blobClient, internalEntityId.CurrentLock?.LeaseId).ConfigureAwait(false);
        internalEntityId.VersionTag = newVersion;
    }

    /// <inheritdoc />
    public virtual async Task Update<TEntity>(TEntity entity)
        where TEntity : IEntity<TEntity>, new()
    {
        logger.LogDebug($"Updating entity {entity.EntityId}");
        var entityIdInternal = (EntityId<TEntity>)entity.EntityId;
        if (!entityIdInternal.Exists)
            throw new InvalidOperationException("Cannot 'Update' new entity, use 'Add' instead");

        if (entityIdInternal.BlobClient is null)
            throw new InvalidOperationException("Entity is missing its 'BlobClient'");

        var contents = JsonSerializer.Serialize(entity);
        logger.LogDebug($"Uploading entity {entity.EntityId} to the storage.");
        var newVersion = await UploadBlobAsync(new MemoryStream(Encoding.UTF8.GetBytes(contents)), versionTag: entityIdInternal.VersionTag, entityIdInternal.BlobClient, entityIdInternal.CurrentLock?.LeaseId);
        entityIdInternal.VersionTag = newVersion;
    }

    /// <inheritdoc />
    public virtual Task AddOrUpdate<TEntity>(TEntity entity) where TEntity : IEntity<TEntity>, new()
        => ((EntityId<TEntity>)entity.EntityId).Exists ? Update(entity) : Add(entity);

    /// <inheritdoc />
    public virtual async Task<IAsyncDisposable> AcquireLock<TEntity>(IEntityId<TEntity> entity)
        where TEntity : IEntity<TEntity>, new()
    {
        var internalEntityId = (EntityId<TEntity>)entity;
        var @lock = await BlobLock.AcquireAsync(internalEntityId.BlobClient, internalEntityId.VersionTag, TimeSpan.FromMinutes(1));
        internalEntityId.CurrentLock = @lock;
        return @lock;
    }

    private static BlobRequestConditions FixedETag(ETag etag)
        => new()
        {
            IfMatch = etag,
        };

    // Generic method to upload the contents of the blob to the storage.
    private static async Task<ETag?> UploadBlobAsync(Stream stream, ETag? versionTag, BlobClient blob, string? leaseId)
    {
        var conditions = new BlobRequestConditions();
        if (versionTag.HasValue)
            // Allows overwrite but only if the version tag matches (prevents concurrent updates)
            conditions.IfMatch = versionTag.Value;
        if (leaseId != null)
            conditions.LeaseId = leaseId;

        var response = await blob.UploadAsync(stream,
            conditions: conditions,
            httpHeaders: new BlobHttpHeaders
            {
                ContentType = "application/json"
            }).ConfigureAwait(false);

        return response.Value.ETag;
    }

    // Automatically creates the entity container if it does not exist yet.
    private async Task<BlobContainerClient> GetOrCreateContainerClient()
    {
        await syncRoot.RunLockedAsync(Task.Run(async () =>
        {
            if (containerClient is null)
            {
                if (serviceClient is null)
                    throw new InvalidOperationException("Cannot use Storage Client without serviceClient set");

                logger.LogDebug($"Creating container {containerName} if it does not exist yet.");
                containerClient = serviceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync().ConfigureAwait(false);
                await containerClient.SetAccessPolicyAsync(PublicAccessType.None).ConfigureAwait(false);
            }
        })).ConfigureAwait(false);

        return containerClient!;
    }
}