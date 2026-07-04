namespace Botyara.Core.Message;

public sealed record BotMessage(
    string Text,
    string ChatId,
    string SenderId,
    object? RawContent,
    CancellationToken CancellationToken = default);
