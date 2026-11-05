using System.Diagnostics.CodeAnalysis;
using NextAuth.Core.Model.Entities;

namespace NextAuth.Core.Model.MafileExport;

public class ExportResult
{
    public string? Error { get; }
    public Dictionary<Mafile, string>? Exported { get; }
    public List<string>? NotFound { get; }
    public List<string>? Conflict { get; }

    [MemberNotNullWhen(true, nameof(Exported))]
    [MemberNotNullWhen(true, nameof(NotFound))]
    [MemberNotNullWhen(true, nameof(Conflict))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool Success => Error == null;

    private ExportResult(string? error, Dictionary<Mafile, string>? exported, List<string>? notFound,
        List<string>? conflict)
    {
        Error = error;
        Exported = exported;
        NotFound = notFound;
        Conflict = conflict;
    }

    public ExportResult(Dictionary<Mafile, string> exported, List<string> notFound, List<string> conflict)
        : this(null, exported, notFound, conflict)
    {
    }


    public ExportResult(string? error)
        : this(error, null, null, null)
    {
    }
}