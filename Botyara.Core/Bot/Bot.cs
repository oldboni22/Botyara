using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Botyara.Core.Connector;
using Botyara.Core.Context;
using Botyara.Core.Message;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Botyara.Core.Bot;

internal sealed class Bot(
    int workersCount,
    IConnector[] connectors,
    IServiceProvider serviceProvider,
    FrozenDictionary<string, IMessageSender> senders,
    Func<IConnector[], PipeContext, Task> messageProcessor,
    ILogger<Bot> logger) : BackgroundService
{
    private readonly ConcurrentStack<Dictionary<object, object?>> _cachedItems = new();
    
    private readonly Channel<BotMessage> _messages = Channel.CreateUnbounded<BotMessage>();
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionTasks = ConnectAsync(stoppingToken);
        var workerTasks = CreateWorkers(stoppingToken);
        
        await Task.WhenAll(workerTasks.Concat(connectionTasks)).ConfigureAwait(false);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var connector in connectors)
        {
            await connector.DisableAsync(cancellationToken).ConfigureAwait(false);
        }
    }
    
    private IEnumerable<Task> CreateWorkers(CancellationToken cancellationToken)
    {
        return Enumerable.Range(0, workersCount)
            .Select(async _ =>
            {
                await foreach (var message in _messages.Reader.ReadAllAsync(cancellationToken))
                {
                    await ProcessMessageAsync(message).ConfigureAwait(false);
                }
            });
    }

    private async Task ProcessMessageAsync(BotMessage message)
    {
        var scope = serviceProvider.CreateScope();
        
        _cachedItems.TryPop(out var items);
        items ??= new();
        
        var sender = senders[message.ConnectorId];

        var context = new PipeContext(
            message,
            scope.ServiceProvider,
            sender,
            items);
        
        try
        {
            await messageProcessor.Invoke(connectors, context).ConfigureAwait(false);
        }
        finally
        {
            scope.Dispose();
            items.Clear();
            _cachedItems.Push(items);
        }
    }
    
    private IEnumerable<Task> ConnectAsync(CancellationToken cancellationToken)
    {
        return connectors.Select(async con =>
        {
            try
            {
                await con.InitializeAsync(cancellationToken);

                await foreach(var message in con.MessageStream.WithCancellation(cancellationToken))
                {
                    await _messages.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                logger.LogConnectorInitError(con.Id, e);
                throw;
            }
        });
    }
}
