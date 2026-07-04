using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Botyara.Core.Message;

namespace Botyara.Core.Connector;

public interface IConnector
{
    IAsyncEnumerable<BotMessage> MessageStream { get; }
    
    Task InitializeAsync(CancellationToken cancellationToken = default);
    
    Task DisableAsync(CancellationToken cancellationToken = default);
    
    IMessageSender MessageSender { get; }
    
    string Id { get; }
}
