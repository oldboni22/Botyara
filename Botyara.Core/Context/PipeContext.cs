using System;
using System.Collections.Generic;
using System.Threading;
using Botyara.Core.Message;

namespace Botyara.Core.Context;

public sealed record PipeContext(
    BotMessage Message,
    IServiceProvider ServiceProvider,
    IMessageSender MessageSender,
    Dictionary<object, object?> Items)
{
    public string ConnectorId => Message.ConnectorId;
    
    public CancellationToken CancellationToken => Message.CancellationToken;
}
