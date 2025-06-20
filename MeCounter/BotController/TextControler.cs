using MeCounter.DataAccess.Postgres.Repositories;
using MeCounter.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MeCounter.BotController
{
    public partial class TgBotController
    {
        private async Task HandleTextMessage(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            if (message?.Text == null || message.From == null || message.Chat == null)
            {
                _logger.LogWarning("Получено некорректное текстовое сообщение: MessageId={MessageId}, ChatId={ChatId}",
                    message?.MessageId, message?.Chat.Id);
                return;
            }

            try
            {
                _logger.LogInformation("Обработка сообщения от {UserName} (ID: {UserId}) в чате {ChatId}: {Message}",
                    message.From.FirstName, message.From.Id, message.Chat.Id, message.Text);

                await EnsureChatAsync(message.Chat, cancellationToken);
                await EnsureUserAsync(message.From, cancellationToken);
                await EnsureChatUserLinkAsync(message.Chat, message.From, cancellationToken);

                var command = NormalizeCommand(message.Text.Split('@')[0]);
                if (_commandHandlers.TryGetValue(command, out var handler))
                {
                    var response = await handler.HandleAsync(message, cancellationToken);
                    await SendBotReplyAsync(botClient, message.Chat.Id, message.MessageId, response, cancellationToken);
                    _logger.LogInformation("Выполнена команда {Command} в чате {ChatId}: {Response}",
                        command, message.Chat.Id, response.Replace("\n", " "));
                    return;
                }

                var textResponse = await _textProcessor.ProcessTextAsync(message, cancellationToken, botClient);
                if (!string.IsNullOrEmpty(textResponse))
                {
                    await SendBotReplyAsync(botClient, message.Chat.Id, message.MessageId, textResponse, cancellationToken);
                    _logger.LogInformation("Обработан текст в чате {ChatId}: {Response}",
                        message.Chat.Id, textResponse.Replace("\n", " "));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке текстового сообщения: UserId={UserId}, ChatId={ChatId}, Text={Text}",
                    message.From.Id, message.Chat.Id, message.Text);
                await SendBotReplyAsync(botClient, message.Chat.Id, message.MessageId,
                    "Произошла ошибка при обработке сообщения.", cancellationToken);
            }
        }
    }
}