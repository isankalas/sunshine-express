using System.Runtime.Serialization;

namespace SunshineExpress.Service.Exceptions;

[Serializable]
public class UnknownWeatherException : Exception
{
    public string City { get; }

    public UnknownWeatherException(string city)
    {
        City = city;
    }

    public UnknownWeatherException(string city, string? message) : base(message)
    {
        City = city;
    }

    public UnknownWeatherException(string city, string? message, Exception? innerException) : base(message, innerException)
    {
        City = city;
    }

    protected UnknownWeatherException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        City = info.GetString(nameof(City)) ?? string.Empty;
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);

        info.AddValue(nameof(City), City);
    }
}