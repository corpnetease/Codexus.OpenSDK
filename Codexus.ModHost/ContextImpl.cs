using System.Diagnostics.CodeAnalysis;
using Codexus.ModSDK;
using Codexus.ModSDK.Event;
using Serilog;

namespace Codexus.ModHost;

public class ContextImpl(ILogger logger, IEventBus eventBus, string? modName = null) : IModContext
{
    private readonly ILogger _logger = modName != null
        ? logger.ForContext("ModName", modName)
        : logger;

    public IEventBus EventBus { get; } = eventBus;

    public void LogInformation([StringSyntax("CompositeFormat")] string message, params object[] args)
    {
        _logger.Information(message, args);
    }

    public void LogWarning([StringSyntax("CompositeFormat")] string message, params object[] args)
    {
        _logger.Warning(message, args);
    }

    public void LogError([StringSyntax("CompositeFormat")] string message, params object[] args)
    {
        _logger.Error(message, args);
    }

    public void LogDebug([StringSyntax("CompositeFormat")] string message, params object[] args)
    {
        _logger.Debug(message, args);
    }

    public void LogError(Exception exception, [StringSyntax("CompositeFormat")] string message, params object[] args)
    {
        _logger.Error(exception, message, args);
    }
}