namespace SunshineExpress.Service.Contract.Storage;

internal class Weather : IEntity
{
    IEntityId IEntity.EntityId => (IEntityId)EntityId;

    public IEntityId<Weather> EntityId { get; }

    public string City { get; init; }

    public double Temperature { get; init; }

    public double Precipitation { get; init; }

    public int WindSpeed { get; init; }

    public string Summary { get; init; }

    public Weather(IEntityId<Weather> entityId, string city, double temperature, double precipitation, int windSpeed, string summary)
    {
        EntityId = entityId;
        City = city;
        Temperature = temperature;
        Precipitation = precipitation;
        WindSpeed = windSpeed;
        Summary = summary;
    }

    public static Weather FromDto(IEntityId<Weather> entityId, WeatherDto dto)
        => new(
            entityId,
            dto.City,
            dto.Temperature,
            dto.Precipitation,
            dto.WindSpeed,
            dto.Summary);

    public WeatherDto ToDto()
        => new(City, Temperature, Precipitation, WindSpeed, Summary);
}
