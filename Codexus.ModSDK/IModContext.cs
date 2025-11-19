using Codexus.ModSDK.Event;

namespace Codexus.ModSDK;

public interface IModContext
{
    IEventBus EventBus { get; }

    void LogInformation(string message, params object[] args);

    void LogWarning(string message, params object[] args);

    void LogError(string message, params object[] args);

    void LogDebug(string message, params object[] args);

    void LogError(Exception exception, string message, params object[] args);
}