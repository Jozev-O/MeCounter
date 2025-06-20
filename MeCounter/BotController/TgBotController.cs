using MeCounter.DataAccess.Postgres.Repositories;
using MeCounter.Interfaces;
using MeCounter.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MeCounter.BotController
{
    public partial class TgBotController
    {
        private readonly ILogger<TgBotController> _logger;
        private readonly ChatsRepository _chatsRepository;
        private readonly UsersRepository _usersRepository;
        private readonly ITextProcessor _textProcessor;
        private readonly Dictionary<string, ICommandHandler> _commandHandlers;

        public TgBotController(
            ILogger<TgBotController> logger,
            ChatsRepository chatsRepository,
            UsersRepository usersRepository,
            DiceStateRepository diceStateRepository,

        ITextProcessor textProcessor,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _chatsRepository = chatsRepository;
            _usersRepository = usersRepository;
            _diceStateRepository = diceStateRepository;
            _textProcessor = textProcessor;
            _commandHandlers = serviceProvider.GetServices<ICommandHandler>()
                .ToDictionary(handler => handler.CommandName, handler => handler);
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update?.Message == null)
            {
                _logger.LogWarning("Получено некорректное обновление: UpdateId={UpdateId}", update?.Id);
                return;
            }

            try
            {
                _logger.LogInformation("Обработка обновления: UpdateId={UpdateId}, Type={Type}, ChatId={ChatId}",
                    update.Id, update.Type, update.Message.Chat.Id);

                switch (update.Type)
                {
                    case UpdateType.Message when update.Message != null:
                        var message = update.Message;
                        switch (message)
                        {
                            case { Text: not null }:
                                await HandleTextMessage(botClient, message, cancellationToken);
                                break;
                            case { Video: not null }:
                                await HandleVideoMessage(botClient, message, cancellationToken);
                                break;
                            case { Dice: not null }:
                                await HandleDiceMessage(botClient, message, cancellationToken);
                                break;
                            default:
                                _logger.LogInformation("Тип сообщения не поддерживается: UpdateId={UpdateId}, ChatId={ChatId}",
                                    update.Id, message.Chat.Id);
                                break;
                        }
                        break;

                    case UpdateType.CallbackQuery when update.CallbackQuery != null:
                        var callbackQuery = update.CallbackQuery;
                        _logger.LogInformation("Получен CallbackQuery: Id={CallbackQueryId}, Data={Data}",
                            callbackQuery.Id, callbackQuery.Data);

                        await botClient.SendMessage(
                            callbackQuery.Id,
                            "Обработка CallbackQuery в разработке.",
                            cancellationToken: cancellationToken);

                        if (callbackQuery.Message != null)
                        {
                            //await SendBotReplyAsync(botClient, callbackQuery.Message.Chat.Id,
                            //    callbackQuery.Message.MessageId,
                            //    "CallbackQuery обработан (временная заглушка).", cancellationToken);
                        }
                        break;

                    default:
                        _logger.LogWarning("Тип обновления не поддерживается: UpdateId={UpdateId}, Type={Type}",
                            update.Id, update.Type);
                        break;
                }
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке обновления: UpdateId={UpdateId}, ChatId={ChatId}",
                    update.Id, update.Message.Chat.Id);
                await HandleErrorAsync(botClient, ex, HandleErrorSource.HandleUpdateError, cancellationToken);
            }
        }

        private string NormalizeCommand(string command) => command.Split('@')[0].ToLower();

        private async Task EnsureChatAsync(Chat tgChat, CancellationToken cancellationToken)
        {
            var dbChat = await _chatsRepository.GetByID(tgChat.Id);
            if (dbChat == null && tgChat.Type != ChatType.Private)
            {
                dbChat = new DataAccess.Postgres.Models.Chat { ChatId = tgChat.Id, Title = tgChat.Title };
                await _chatsRepository.Add(dbChat);
                _logger.LogInformation("Создан чат с ID {ChatId}", tgChat.Id);
            }
        }

        private async Task EnsureUserAsync(User tgUser, CancellationToken cancellationToken)
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

        private async Task EnsureChatUserLinkAsync(Chat tgChat, User tgUser, CancellationToken cancellationToken)
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
            if (string.IsNullOrEmpty(message)) return;
            await botClient.SendMessage(
                chatId,
                message,
                replyParameters: reply ? new ReplyParameters { MessageId = replyToMessageId } : null,
                cancellationToken: cancellationToken);
        }
    }
}