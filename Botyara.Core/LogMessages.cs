using Microsoft.Extensions.Logging;

namespace Botyara.Core;

internal static partial class LogMessages
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Error,
        Message = "Failed to start a connector: {ConnectorId}"
    )]
    public static partial void LogConnectorInitError(this ILogger logger, string connectorId, Exception exception);
}
