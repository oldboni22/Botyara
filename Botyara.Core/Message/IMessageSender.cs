namespace Botyara.Core.Message;

public interface IMessageSender
{
    ValueTask SendAsync(object parameters);
}
