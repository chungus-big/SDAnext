using NextAuth.Core.Services;

namespace NextAuth.Core.Utility;

public class ClipboardHelper
{
    public static async Task<bool> Set(string text)
    {
        try
        {
            if (AppServices.Clipboard != null)
            {
                return await AppServices.Clipboard.SetTextAsync(text);
            }
            return false;
        }
        catch
        {
            SnackbarController.SendError(LocManager.GetCommonOrDefault("Error", "Error"));
            return false;
        }
    }

    public static async Task<bool> SetFiles(IEnumerable<string> files)
    {
        try
        {
            if (AppServices.Clipboard != null)
            {
                return await AppServices.Clipboard.SetFilesAsync(files);
            }
            return false;
        }
        catch
        {
            SnackbarController.SendError(LocManager.GetCommonOrDefault("Error", "Error"));
            return false;
        }
    }
}