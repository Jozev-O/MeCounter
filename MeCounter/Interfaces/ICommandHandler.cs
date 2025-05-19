namespace MeCounter.Interfaces
{
    public interface ICommandHandler
    {
        Task<string> HandleAsync(Telegram.Bot.Types.Message message, CancellationToken cancellationToken);
    }
}