namespace NextAuth.Core.Services;

public interface IAppService
{
    void OpenFolder(string path);
    void OpenUrl(string url);
    void Shutdown();
    bool IsMainWindowMinimized();
    void PostToUI(Action action);
}
