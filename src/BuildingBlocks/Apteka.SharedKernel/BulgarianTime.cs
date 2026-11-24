namespace Apteka.SharedKernel;

/// <summary>Times are stored in UTC and shown in Europe/Sofia (rule 9). Dates such as „today“ are Sofia dates.</summary>
public static class BulgarianTime
{
    public static TimeZoneInfo Zone { get; } = TimeZoneInfo.FindSystemTimeZoneById("Europe/Sofia");

    public static DateTimeOffset ToLocal(DateTimeOffset utc) => TimeZoneInfo.ConvertTime(utc, Zone);

    public static DateOnly Today(DateTimeOffset utcNow) => DateOnly.FromDateTime(ToLocal(utcNow).DateTime);
}
