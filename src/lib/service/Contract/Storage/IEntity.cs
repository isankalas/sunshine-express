namespace SunshineExpress.Service.Contract.Storage;

public interface IEntity<TEntity> where TEntity : IEntity<TEntity>
{
    IEntityId EntityId { get; }

    void SetEntityId(IEntityId<TEntity> entityId);
}