namespace SteamLib.Core.StatusCodes;

public abstract class Enumeration<T>(int id, string name) : IComparable<Enumeration<T>>, IEquatable<Enumeration<T>>
    where T : Enumeration<T>
{
    private static readonly Lazy<T[]> AllItems = new(() =>
        [.. typeof(T)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly)
            .Where(f => typeof(T).IsAssignableFrom(f.FieldType))
            .Select(f => f.GetValue(null))
            .OfType<T>()]);

    private static readonly Lazy<Dictionary<int, T>> IdLookup = new(() =>
    {
        var dict = new Dictionary<int, T>();
        foreach (var item in AllItems.Value)
        {
            dict.TryAdd(item.Id, item);
        }
        return dict;
    });

    private static readonly Lazy<Dictionary<string, T>> NameLookup = new(() =>
    {
        var dict = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in AllItems.Value)
        {
            dict.TryAdd(item.Name, item);
        }
        return dict;
    });

    public int Id { get; } = id;
    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));

    public override string ToString() => Name;

    public static IEnumerable<T> GetAll() => AllItems.Value;

    public static T? FromId(int id) =>
        IdLookup.Value.TryGetValue(id, out var item) ? item : null;

    public static T? FromName(string? name) =>
        name is not null && NameLookup.Value.TryGetValue(name, out var item) ? item : null;

    public override bool Equals(object? obj) =>
        obj is Enumeration<T> other && Equals(other);

    public bool Equals(Enumeration<T>? other) =>
        other is not null && GetType() == other.GetType() && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();

    public int CompareTo(Enumeration<T>? other) =>
        other is null ? 1 : Id.CompareTo(other.Id);

    public static bool operator ==(Enumeration<T>? left, Enumeration<T>? right) =>
        ReferenceEquals(left, right) || (left is not null && right is not null && left.Equals(right));

    public static bool operator !=(Enumeration<T>? left, Enumeration<T>? right) => !(left == right);
}
