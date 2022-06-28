namespace SunshineExpress.Service.Contract;

public record WeatherDto(string City, double Temperature, double Precipitation, int WindSpeed, string Summary)
{
    public string City { get; init; } = City;

    public double Temperature { get; init; } = Temperature;

    public double Precipitation { get; init; } = Precipitation;

    public int WindSpeed { get; init; } = WindSpeed;

    public string Summary { get; init; } = Summary;
}