using Telegram.Bot;
namespace MeCounter.Interfaces
{
    public interface ITextProcessor
    {
        Task<string?> ProcessTextAsync(Telegram.Bot.Types.Message message, CancellationToken cancellationToken, ITelegramBotClient botClient);
    }
}