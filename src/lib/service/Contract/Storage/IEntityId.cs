namespace SunshineExpress.Service.Contract.Storage;

public interface IEntityId<TEntity> : IEntityId where TEntity : IEntity<TEntity>
{
}

public interface IEntityId
{
}