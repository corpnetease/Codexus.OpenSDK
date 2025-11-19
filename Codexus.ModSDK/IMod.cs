namespace Codexus.ModSDK;

public interface IMod
{
    void OnLoad(IModContext context);
    void OnUnload();
}