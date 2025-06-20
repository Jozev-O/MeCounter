using Telegram.Bot.Types;

namespace MeCounter.Interfaces
{
    public interface ICommandHandler
    {
        string CommandName { get; }
        Task<string> HandleAsync(Message message, CancellationToken cancellationToken);
    }
}