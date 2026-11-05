using NextAuth.Core.Model.MafileExport;
using NextAuth.Core.Model.Mafiles;
using SteamLib.SteamMobile;

namespace NextAuth.Core.Model;

public static class Shell
{
    public static async Task Initialize()
    {
        LocManager.Init();
        LocManager.SetApplicationLocalization(Settings.Instance.Language);

        try
        {
            await TimeAligner.AlignTimeAsync();
        }
        catch
        {
            TimeAligner.SetTimeDifference(0);
        }

        var threads = Environment.ProcessorCount > 0 ? Environment.ProcessorCount : 1;
        await Storage.Initialize(threads);
        MafileExporterStorage.Initialize();
    }
}
