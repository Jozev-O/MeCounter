using MeCounter.DataAccess.Postgres.Repositories;
using MeCounter.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace MeCounter.Commands
{
    public class PornCommandHandler(
        ILogger<PornCommandHandler> logger,
        PornRepository pornRepository,
        UsersRepository userRepository
        ) : ICommandHandler
    {
        private readonly ILogger<PornCommandHandler> _logger = logger;
        private readonly PornRepository _pornRepository = pornRepository;
        private readonly UsersRepository _userRepository = userRepository;

        public async Task<string> HandleAsync(Message message, CancellationToken cancellationToken)
        {
            if (message?.Text == null) return "Команда некорректна.";

            var parts = message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return "Укажи ссылку: /яйца [ссылка]";

            var chatId = message.Chat.Id;
            var userDB = await _userRepository.GetByID(message.From.Id);
            if (userDB == null || !userDB.IsAdmin)
            {
                _logger.LogWarning("Неразрешенный чат для команды {ChatId}", chatId);
                return "Тебе нельзя.";
            }

            var url = parts[1];
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uriResult) ||
                (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            {
                _logger.LogWarning("Некорректная ссылка в чате {ChatId}: {Url}", chatId, url);
                return "Кидай нормальную ссылку: /яйца [ссылка]";
            }

            await _pornRepository.Add(new DataAccess.Postgres.Models.Porn { Url = uriResult });
            _logger.LogInformation("Добавлена ссылка в чате {ChatId}: {Url}", chatId, uriResult.OriginalString);
            return "Ссылка добавлена!";
        }
    }
}