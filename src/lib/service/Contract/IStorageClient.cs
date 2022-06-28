using SunshineExpress.Service.Contract.Storage;

namespace SunshineExpress.Service.Contract;

/// <summary>
/// Declares the storage methods necessary for the service to run.
/// </summary>
public interface IStorageClient
{
    /// <summary>
    /// Creates an object that represents an entity in the storage without holding the entity data.
    /// </summary>
    /// <typeparam name="TEntity">Type of the entity.</typeparam>
    /// <param name="entityKey">Unique entity identifier.</param>
    /// <returns>An object that represents the entity.</returns>
    IEntityId<TEntity> CreateEntityId<TEntity>(string entityKey) where TEntity : IEntity<TEntity>, new();

    /// <summary>
    /// Locks the entity and returns a handle that is used to release the lock when a concurrent operation has completed.
    /// </summary>
    /// <typeparam name="TEntity">Type of the entity to lock.</typeparam>
    /// <param name="entityKey">Identifier of the entity to acquire the lock for.</param>
    /// <returns>A disposable object which releases the lock when disposed.</returns>
    Task<IAsyncDisposable> AcquireLock<TEntity>(IEntityId<TEntity> entityKey) where TEntity : IEntity<TEntity>, new();

    /// <summary>
    /// Adds the entity to the storage or updates its value if it already exists.
    /// </summary>
    /// <typeparam name="TEntity">Type of the entity.</typeparam>
    /// <param name="entity">Entity ot add or update in the storage.</param>
    Task AddOrUpdate<TEntity>(TEntity entity) where TEntity : IEntity<TEntity>, new();

    /// <summary>
    /// Fetches the entity data from the storage.
    /// </summary>
    /// <typeparam name="TEntity">Type of the entity to fetch.</typeparam>
    /// <param name="entityId">Unique entity identifier.</param>
    /// <returns>The entity with all its data.</returns>
    Task<TEntity> Get<TEntity>(IEntityId<TEntity> entityId) where TEntity : IEntity<TEntity>, new();
}