using MeCounter.DataAccess.Postgres.Repositories;
using MeCounter.Interfaces;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace MeCounter.Commands
{
    public class SchetchikCommandHandler(ChatsRepository chatsRepository, UsersRepository usersRepository) : ICommandHandler
    {
        private readonly ChatsRepository _chatsRepository = chatsRepository
            ?? throw new ArgumentNullException(nameof(chatsRepository));

        private readonly UsersRepository _usersRepository = usersRepository
            ?? throw new ArgumentNullException(nameof(usersRepository));

        public async Task<string> HandleAsync(Message message, CancellationToken cancellationToken)
        {
            if (message == null) return "Сообщение не распознано.";

            var chatId = message.Chat.Id;
            var members = await _chatsRepository.GetUsersByChatId(chatId);
            if (members == null || members.Count == 0)
            {
                return "Участники чата не найдены или не зарегистрированы.";
            }

            var sb = new StringBuilder();
            foreach (var member in members)
            {
                var userName = GetDisplayName(member);
                var wordCount = await _usersRepository.GetUserWordCountAsync(member.UserId);
                sb.AppendLine($"{userName}: {wordCount}");
            }

            return sb.ToString().Trim();
        }

        private static string GetDisplayName(DataAccess.Postgres.Models.User user)
        {
            return user.Username != null ? $"{user.Username}"
                 : !string.IsNullOrEmpty(user.FirstName) || !string.IsNullOrEmpty(user.LastName)
                   ? $"{user.FirstName} {user.LastName}".Trim()
                   : user.UserId.ToString();
        }
    }
}