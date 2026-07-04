using Botyara.Core.Message;

namespace Botyara.Core.Connector;

public interface IConnector
{
    IAsyncEnumerable<BotMessage> MessageStream { get; }
    
    ValueTask InitializeAsync(CancellationToken cancellationToken = default);
    
    
}
