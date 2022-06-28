namespace SunshineExpress.Service.Contract.Storage;

public interface IStorageClient
{
    IEntityId<TEntity> CreateEntityId<TEntity>(string entityKey) where TEntity : IEntity;

    Task<IAsyncDisposable> AcquireLock<TEntity>(IEntityId<TEntity> entityKey) where TEntity : IEntity;

    Task AddOrUpdate<TEntity>(TEntity entity) where TEntity : IEntity;

    Task<TEntity> Get<TEntity>(IEntityId<TEntity> entityId) where TEntity : IEntity;
}