using MeCounter.DataAccess.Postgres.Models;
using MeCounter.DataAccess.Postgres.Repositories;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MeCounter.BotController
{
    public partial class TgBotController
    {
        private readonly List<string> _monikerList = ["сынище", "чурка", "шайтан", "бешбармак", "чорт"];
        private readonly DiceStateRepository _diceStateRepository;

        public async Task HandleDiceMessage(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            if (message?.Dice == null || message.Chat == null)
            {
                _logger.LogWarning("Некорректное сообщение с кубиком: MessageId={MessageId}, ChatId={ChatId}",
                    message?.MessageId, message?.Chat.Id);
                return;
            }

            try
            {
                string reply = string.Empty;
                switch (message.Dice.Emoji)
                {
                    case "🎲":
                        reply = await HandleDiceRoll(message, cancellationToken);
                        break;
                    case "🎰":
                        reply = "Слоты пока не поддерживаются, попробуй кубик! 🎲";
                        break;
                    case "🎯":
                        reply = "Дартс пока не поддерживаются, попробуй кубик! 🎲";
                        break;
                    case "🏀":
                        reply = "Баскетбол пока не поддерживаются, попробуй кубик! 🎲";
                        break;
                    case "⚽️":
                        reply = "Футбол пока не поддерживаются, попробуй кубик! 🎲";
                        break;
                    case "🎳":
                        reply = "Боулинг пока не поддерживаются, попробуй кубик! 🎲";
                        break;
                    default:
                        reply = "Этот тип кубика пока не поддерживается.";
                        break;
                }


                if (!string.IsNullOrEmpty(reply))
                {
                    await botClient.SendMessage(
                        message.Chat.Id,
                        reply,
                        cancellationToken: cancellationToken);
                    _logger.LogInformation("Отправлен ответ на кубик: ChatId={ChatId}, Reply={Reply}",
                        message.Chat.Id, reply.Replace("\n", " "));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке кубика: MessageId={MessageId}, ChatId={ChatId}",
                    message.MessageId, message.Chat.Id);
                await SendBotReplyAsync(botClient, message.Chat.Id, message.MessageId,
                    $"Произошла ошибка при обработке кубика.\n{ex}", cancellationToken);
            }
        }

        private async Task<string> HandleDiceRoll(Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var userName = message.From?.FirstName ?? message.From?.Username ?? "Неизвестный";
            var currentValue = message.Dice!.Value;

            var state = await _diceStateRepository.GetByID(chatId);
            var reply = string.Empty;

            if (state != null && !string.IsNullOrEmpty(state.LastUser) && !string.IsNullOrEmpty(state.LastMoniker))
            {
                if (currentValue == state.LastValue)
                {
                    reply = $"Ура, {state.LastUser} теперь {state.LastMoniker}!";
                }
                else
                {
                    reply = $"Анлак...\nЖиви, {state.LastUser}!";
                }
            }

            // Обновляем состояние
            var rnd = new Random();
            var users = await _usersRepository.GetUsersByChatId(chatId);
            if (users == null || users.Count == 0)
            {
                _logger.LogWarning("Пользователи не найдены в чате: ChatId={ChatId}", chatId);
                return "В чате нет зарегистрированных пользователей.";
            }

            var newUser = users[rnd.Next(users.Count)];
            var newMoniker = _monikerList[rnd.Next(_monikerList.Count)];
            var newValue = rnd.Next(1, 7);

            var newState = new DiceState
            {
                ChatId = chatId,
                LastValue = newValue,
                LastUser = newUser.FirstName ?? newUser.Username,
                LastMoniker = newMoniker
            };

            if (state == null)
            {
                await _diceStateRepository.Add(newState);
                _logger.LogInformation("Создано состояние кубика для чата: ChatId={ChatId}", chatId);
            }
            else
            {
                await _diceStateRepository.Update(newState);
                _logger.LogInformation("Обновлено состояние кубика для чата: ChatId={ChatId}", chatId);
            }

            reply += $"\nСледующий: если выпадет [{newValue}], {newUser.FirstName ?? newUser.Username} станет {newMoniker}!";
            return reply;
        }
    }
}
