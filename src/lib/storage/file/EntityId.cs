using SunshineExpress.Service.Contract.Storage;
using System.Text.RegularExpressions;

namespace SunshineExpress.Storage.Memory;

public class EntityId<TEntity> : IEntityId<TEntity> where TEntity : IEntity<TEntity>
{
    private EntityId(string id)
    {
        EntityType = typeof(TEntity).Name;
        Id = id;
    }

    public string EntityType { get; }

    public string Id { get; }

    private static readonly Regex entityIdInvalidCharacters = new("[^A-Za-z0-9-_\\$]+");
    public static EntityId<TEntity> Create(string id)
    {
        id = entityIdInvalidCharacters.Replace(id, string.Empty);
        return new EntityId<TEntity>(id);
    }

    public override string ToString()
        => $"{EntityType}/{Id}";
}
