using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace MeCounter.BotController
{
    public partial class TgBotController
    {
        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source,
           CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Ошибка обработки: {Source}", source);
            _logger.LogError("exception: {exception}", exception.Message);
            return Task.CompletedTask;
        }
    }
}
