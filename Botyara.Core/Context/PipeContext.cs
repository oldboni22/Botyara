using Botyara.Core.Message;

namespace Botyara.Core.Context;

public sealed record PipeContext(
    BotMessage Message,
    IServiceProvider ServiceProvider,
    IMessageSender MessageSender,
    Dictionary<object, object?> Items)
{
    public CancellationToken CancellationToken => Message.CancellationToken;
}
