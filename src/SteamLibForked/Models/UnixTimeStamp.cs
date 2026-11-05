namespace SteamLib.Models;

public enum UnixFormat
{
    Seconds,
    Milliseconds
}

public readonly struct UnixTimeStamp(long value, UnixFormat format = UnixFormat.Seconds) : IEquatable<UnixTimeStamp>, IComparable<UnixTimeStamp>
{
    private readonly long _seconds = format == UnixFormat.Milliseconds ? value / 1000 : value;

    public long Value => _seconds;

    public DateTime Time
    {
        get
        {
            try
            {
                return DateTimeOffset.FromUnixTimeSeconds(_seconds).UtcDateTime;
            }
            catch (ArgumentOutOfRangeException)
            {
                return _seconds < 0 ? DateTime.MinValue : DateTime.MaxValue;
            }
        }
    }

    public DateTime UtcDateTime => Time;

    public DateTime LocalDateTime => ToLocalDateTime();

    public static UnixTimeStamp FromDateTime(DateTime dateTime)
    {
        if (dateTime.Kind == DateTimeKind.Unspecified)
        {
            return new UnixTimeStamp(new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)).ToUnixTimeSeconds());
        }

        return new UnixTimeStamp(new DateTimeOffset(dateTime).ToUnixTimeSeconds());
    }

    public static UnixTimeStamp FromDateTimeOffset(DateTimeOffset dateTimeOffset) =>
        new(dateTimeOffset.ToUnixTimeSeconds());

    public static UnixTimeStamp UtcNow =>
        new(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

    public long ToLong() => _seconds;

    public DateTime ToLocalDateTime()
    {
        try
        {
            return DateTimeOffset.FromUnixTimeSeconds(_seconds).LocalDateTime;
        }
        catch (ArgumentOutOfRangeException)
        {
            return _seconds < 0 ? DateTime.MinValue : DateTime.MaxValue;
        }
    }

    public DateTime ToUtcDateTime() => Time;

    public override string ToString() => _seconds.ToString();

    public bool Equals(UnixTimeStamp other) => _seconds == other._seconds;

    public override bool Equals(object? obj) => obj is UnixTimeStamp other && Equals(other);

    public override int GetHashCode() => _seconds.GetHashCode();

    public int CompareTo(UnixTimeStamp other) => _seconds.CompareTo(other._seconds);

    public static bool operator ==(UnixTimeStamp left, UnixTimeStamp right) => left.Equals(right);

    public static bool operator !=(UnixTimeStamp left, UnixTimeStamp right) => !left.Equals(right);

    public static bool operator <(UnixTimeStamp left, UnixTimeStamp right) => left._seconds < right._seconds;

    public static bool operator <=(UnixTimeStamp left, UnixTimeStamp right) => left._seconds <= right._seconds;

    public static bool operator >(UnixTimeStamp left, UnixTimeStamp right) => left._seconds > right._seconds;

    public static bool operator >=(UnixTimeStamp left, UnixTimeStamp right) => left._seconds >= right._seconds;

    public static UnixTimeStamp operator +(UnixTimeStamp timestamp, TimeSpan timeSpan) =>
        new(timestamp._seconds + (long)timeSpan.TotalSeconds);

    public static UnixTimeStamp operator -(UnixTimeStamp timestamp, TimeSpan timeSpan) =>
        new(timestamp._seconds - (long)timeSpan.TotalSeconds);

    public static TimeSpan operator -(UnixTimeStamp left, UnixTimeStamp right) =>
        TimeSpan.FromSeconds(left._seconds - right._seconds);

    public static implicit operator long(UnixTimeStamp timestamp) => timestamp._seconds;

    public static implicit operator UnixTimeStamp(long seconds) => new(seconds);
}
