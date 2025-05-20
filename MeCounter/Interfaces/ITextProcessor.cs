using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
namespace MeCounter.Interfaces
{
    public interface ITextProcessor
    {
        Task<string?> ProcessTextAsync(Message message, CancellationToken cancellationToken, ITelegramBotClient botClient);
    }
}