using Microsoft.Extensions.Logging;
using SunshineExpress.Service.Contract;
using SunshineExpress.Service.Contract.Storage;
using SunshineExpress.Storage.Memory.Configuration;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace SunshineExpress.Storage.Memory;

public class FileStorage : IStorageClient
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> locks = new();
    private readonly string basePath;
    private readonly ILogger<FileStorage> logger;

    public FileStorage(FileStorageConfiguration configuration, ILogger<FileStorage> logger)
    {
        this.basePath = configuration.BasePath;
        this.logger = logger;
    }

    public async Task<IAsyncDisposable> AcquireLock<TEntity>(IEntityId<TEntity> entityKey) where TEntity : IEntity<TEntity>, new()
    {
        var semaphore = locks.GetOrAdd(entityKey.ToString()!, key => new SemaphoreSlim(1));
        return await SemaphoreWrapperLock.AcquireLock(semaphore);
    }

    public Task AddOrUpdate<TEntity>(TEntity entity) where TEntity : IEntity<TEntity>, new()
    {
        var path = Path.Combine(basePath, entity.EntityId.ToString()!);
        var directory = new FileInfo(path).Directory!;
        if (!directory.Exists)
            directory.Create();
        return File.WriteAllTextAsync(path, JsonSerializer.Serialize(entity), Encoding.UTF8);
    }

    public IEntityId<TEntity> CreateEntityId<TEntity>(string entityKey) where TEntity : IEntity<TEntity>, new()
        => EntityId<TEntity>.Create(entityKey);

    public async Task<TEntity> Get<TEntity>(IEntityId<TEntity> entityId) where TEntity : IEntity<TEntity>, new()
    {
        var path = Path.Combine(basePath, entityId.ToString()!);
        var fileInfo = new FileInfo(path);
        if (fileInfo.Exists) {
            var content = await File.ReadAllTextAsync(Path.Combine(basePath, entityId.ToString()!), Encoding.UTF8);
            return JsonSerializer.Deserialize<TEntity>(content)!;
        }
        
        return new TEntity();
    }
}