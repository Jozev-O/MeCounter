using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace MeCounter.BotController
{
    public partial class TgBotController
    {
        private async Task HandleVideoMessage(ITelegramBotClient botClient, Message? message, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Полученов видео от {ChatId}:", message.From?.Id);

            var command = NormalizeCommand(message.Caption.Split(' ')[0]);
            if (_commandHandlers.TryGetValue(command, out var handler))
            {
                var response = await handler.HandleAsync(message, cancellationToken);
                await SendBotReplyAsync(botClient, message.Chat.Id, message.MessageId, response, cancellationToken);
                _logger.LogInformation("Команда {Command} в чате {ChatId}: {Response}", command, message.Id, response.Replace("\n", " "));
                return;
            }
        }
    }
}
