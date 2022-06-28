using System.Runtime.Serialization;

namespace SunshineExpress.Service.Exceptions;

/// <summary>
/// Used to indicate an invalid operation attempt in the application.
/// </summary>
[Serializable]
public class UnknownCityException : Exception
{
    public string City { get; }

    public UnknownCityException(string city) : base($"Unknown city: {city}")
    {
        City = city;
    }

    public UnknownCityException(string city, string? message) : base(message)
    {
        City = city;
    }

    public UnknownCityException(string city, string? message, Exception? innerException) : base(message, innerException)
    {
        City = city;
    }

    protected UnknownCityException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        City = info.GetString(nameof(City)) ?? string.Empty;
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);

        info.AddValue(nameof(City), City);
    }
}