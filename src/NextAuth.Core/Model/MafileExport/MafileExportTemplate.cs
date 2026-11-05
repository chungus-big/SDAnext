namespace NextAuth.Core.Model.MafileExport;

public class MafileExportTemplate
{
    public string Name { get; set; } = string.Empty;
    public bool UseLoginAsMafileName { get; set; }
    public bool IncludeSharedSecret { get; set; }
    public bool IncludeIdentitySecret { get; set; }
    public bool IncludeRCode { get; set; }
    public bool IncludeSessionData { get; set; }
    public bool IncludeOtherInfo { get; set; }
    public bool IncludeNextPassword { get; set; }
    public string? Path { get; set; }
    public bool ExportToZip { get; set; }
}