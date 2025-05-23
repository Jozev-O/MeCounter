using MeCounter.Commands;
using MeCounter.DataAccess.Postgres.Repositories;
using MeCounter.Interfaces;
using MeCounter.Services;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MeCounter
{
    public class TgBotController
    {
        private readonly ILogger<TgBotController> _logger;
        private readonly ChatsRepository _chatsRepository;
        private readonly UsersRepository _usersRepository;
        private readonly TextProcessor _textProcessor;
        private readonly Dictionary<string, ICommandHandler> _commandHandlers;

        public TgBotController(
            ILogger<TgBotController> logger,
            ChatsRepository chatsRepository,
            UsersRepository usersRepository,
            TextProcessor textProcessor,
            SchetchikCommandHandler schetchikHandler,
            NoSchetchikCommandHandler noSchetchikHandler,
            PornCommandHandler pornHandler,
            AdminCommandHandler adminHandler,
            VersionCommandHandler vertionHandler,
            VideoCommandHandler videoHandler
            )
        {
            _logger = logger;
            _chatsRepository = chatsRepository;
            _usersRepository = usersRepository;
            _textProcessor = textProcessor;
            _commandHandlers = new Dictionary<string, ICommandHandler>
            {
                { "/admin", adminHandler },
                { "/яйца", pornHandler },
                { "/schetchik", schetchikHandler },
                { "/no_schetchik", noSchetchikHandler },
                { "/version", vertionHandler },
                { "/savevideo", videoHandler},
                { "/sendvideo", videoHandler }
            };
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // Проверяем, есть ли видео
            if (update.Message?.Video != null)
            {
                _logger.LogInformation("Полученов видео от {ChatId}:", update.Message?.From?.Id);

                await HandleVideoMessageAsync(botClient, update.Message, cancellationToken);
                return;
            }
            if (update.Message is not { Text: var textRaw and not null, From: var tgUser, Chat: var tgChat }) return;

            _logger.LogInformation("Сообщение от {UserName} (ID: {UserId}) в чате {ChatId}: {Message}",
                tgUser.FirstName, tgUser.Id, tgChat.Id, textRaw);

            await EnsureChatAsync(tgChat, cancellationToken);
            await EnsureUserAsync(tgUser, cancellationToken);
            await EnsureChatUserLinkAsync(tgChat, tgUser, cancellationToken);

            var command = NormalizeCommand(textRaw.Split(' ')[0]);
            if (_commandHandlers.TryGetValue(command, out var handler))
            {
                var response = command == "/sendvideo"
                    ? await ((VideoCommandHandler)handler).SendVideoAsync(update.Message, cancellationToken)
                    : await handler.HandleAsync(update.Message, cancellationToken);             // Костыль. Потом исправить
                //var response = await handler.HandleAsync(update.Message, cancellationToken);
                await SendBotReplyAsync(botClient, tgChat.Id, update.Message.MessageId, response, cancellationToken);
                _logger.LogInformation("Команда {Command} в чате {ChatId}: {Response}", command, tgChat.Id, response.Replace("\n", " "));
                return;
            }

            var textResponse = await _textProcessor.ProcessTextAsync(update.Message, cancellationToken, botClient);
            if (textResponse != null)
            {
                await SendBotReplyAsync(botClient, tgChat.Id, update.Message.MessageId, textResponse, cancellationToken);
                _logger.LogInformation("Текст в чате {ChatId}: {Response}", tgChat.Id, textResponse.Replace("\n", " "));
            }
        }

        private async Task HandleVideoMessageAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            if (_commandHandlers.TryGetValue("/savevideo", out var handler))
            {
                var response = await ((VideoCommandHandler)handler).HandleAsync(message, cancellationToken);
                await SendBotReplyAsync(botClient, message.Chat.Id, message.MessageId, response, cancellationToken);
                _logger.LogInformation("Видео обработано в чате {ChatId}: {Response}", message.Chat.Id, response.Replace("\n", " "));
            }
        }

        private string NormalizeCommand(string command) => command.Split('@')[0].ToLower();

        private async Task EnsureChatAsync(Telegram.Bot.Types.Chat tgChat, CancellationToken cancellationToken)
        {
            var dbChat = await _chatsRepository.GetByID(tgChat.Id);
            if (dbChat == null && tgChat.Type != ChatType.Private)
            {
                dbChat = new DataAccess.Postgres.Models.Chat { ChatId = tgChat.Id, Title = tgChat.Title };
                await _chatsRepository.Add(dbChat);
                _logger.LogInformation("Создан чат с ID {ChatId}", tgChat.Id);
            }
        }

        private async Task EnsureUserAsync(Telegram.Bot.Types.User tgUser, CancellationToken cancellationToken)
        {
            var dbUser = await _usersRepository.GetByID(tgUser.Id);
            if (dbUser == null)
            {
                dbUser = new DataAccess.Postgres.Models.User
                {
                    UserId = tgUser.Id,
                    FirstName = tgUser.FirstName,
                    LastName = tgUser.LastName,
                    Username = tgUser.Username,
                    IsAdmin = false,
                    IsCounted = true,
                    WordCount = 0
                };
                await _usersRepository.Add(dbUser);
                _logger.LogInformation("Создан пользователь с ID {UserId}", tgUser.Id);
            }
        }

        private async Task EnsureChatUserLinkAsync(Telegram.Bot.Types.Chat tgChat, Telegram.Bot.Types.User tgUser, CancellationToken cancellationToken)
        {
            if (tgChat.Type == ChatType.Private) return;

            var dbChat = await _chatsRepository.GetByID(tgChat.Id);
            var dbUser = await _usersRepository.GetByID(tgUser.Id);
            if (dbChat != null && dbUser != null && !dbChat.Users.Any(u => u.UserId == dbUser.UserId))
            {
                dbChat.Users.Add(dbUser);
                await _chatsRepository.Update(dbChat);
                _logger.LogInformation("Связь между чатом {ChatId} и пользователем {UserId}", tgChat.Id, tgUser.Id);
            }
        }

        private static async Task SendBotReplyAsync(ITelegramBotClient botClient, long chatId, int replyToMessageId,
            string message, CancellationToken cancellationToken, bool reply = true)
        {
            await botClient.SendMessage(
                chatId,
                message,
                replyParameters: reply ? new ReplyParameters { MessageId = replyToMessageId } : null,
                cancellationToken: cancellationToken);
        }

        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Ошибка обработки: {Source}", source);
            _logger.LogError("exception: {exception}", exception.Message);
            return Task.CompletedTask;
        }
    }
}