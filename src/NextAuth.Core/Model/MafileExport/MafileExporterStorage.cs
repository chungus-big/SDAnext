using System.Collections.ObjectModel;
using System.Text.Json;

namespace NextAuth.Core.Model.MafileExport;

public static class MafileExporterStorage
{
    private const string FILE_NAME = "export_templates.json";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    public static ObservableCollection<MafileExportTemplate> Templates { get; } = [];


    public static void Initialize()
    {
        LoadOrCreateTemplate();
    }

    public static MafileExportTemplate? GetTemplate(string name)
    {
        return Templates.FirstOrDefault(x => x.Name == name);
    }

    public static void AddTemplate(MafileExportTemplate template)
    {
        Templates.Add(template);
        Save();
    }

    public static void DeleteTemplate(MafileExportTemplate template)
    {
        if (Templates.Remove(template))
        {
            Save();
        }
    }

    private static void LoadOrCreateTemplate()
    {
        if (!File.Exists(FILE_NAME))
        {
            Templates.Clear();
            foreach (var mafileExportTemplate in CreateDefaultTemplate())
            {
                Templates.Add(mafileExportTemplate);
            }

            Save();
            return;
        }

        var json = File.ReadAllText(FILE_NAME);
        try
        {
            var loadedTemplates = JsonSerializer.Deserialize<List<MafileExportTemplate>>(json, JsonOptions);
            if (loadedTemplates != null)
            {
                Templates.Clear();
                foreach (var tmpl in loadedTemplates)
                {
                    Templates.Add(tmpl);
                }
            }
        }
        catch
        {
            Templates.Clear();
            Save();
        }
    }

    public static void Save()
    {
        var json = JsonSerializer.Serialize(Templates, JsonOptions);
        File.WriteAllText(FILE_NAME, json);
    }

    private static IEnumerable<MafileExportTemplate> CreateDefaultTemplate()
    {
        return
        [
            new MafileExportTemplate
            {
                Name = "Default",
                UseLoginAsMafileName = false,
                IncludeSharedSecret = true,
                IncludeIdentitySecret = true,
                IncludeRCode = true,
                IncludeSessionData = true,
                IncludeOtherInfo = true,
                IncludeNextPassword = true,
                Path = null
            },
            new MafileExportTemplate
            {
                Name = "Auth only",
                UseLoginAsMafileName = true,
                IncludeSharedSecret = true,
                IncludeIdentitySecret = false,
                IncludeRCode = false,
                IncludeSessionData = false,
                IncludeOtherInfo = false,
                IncludeNextPassword = false,
                Path = null
            }
        ];
    }
}