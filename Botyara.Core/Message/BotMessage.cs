using System.Threading;

namespace Botyara.Core.Message;

public sealed record BotMessage(
    string Text,
    string ChatId,
    string SenderId,
    string ConnectorId,
    object? RawContent,
    CancellationToken CancellationToken = default);
