namespace SunshineExpress.Service.Contract.Storage;

public interface IEntityId<TEntity> : IEntity where TEntity : IEntity
{
}

public interface IEntityId
{
}