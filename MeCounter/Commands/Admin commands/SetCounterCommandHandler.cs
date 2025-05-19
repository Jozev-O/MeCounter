using MeCounter.DataAccess.Postgres.Repositories;
using MeCounter.Interfaces;

namespace MeCounter.Commands.Admin_commands
{
    public class SetCounterCommandHandler(UsersRepository usersRepository) : IAdminCommandHandler
    {
        private readonly UsersRepository _usersRepository = usersRepository;

        public string CommandName => "set_counter";

        public async Task<string> HandleAsync(string[] args, long adminId, CancellationToken cancellationToken)
        {
            if (args.Length < 2) return "Используйте: set_counter [user_id] [value]";
            if (!long.TryParse(args[0], out long userId) || !int.TryParse(args[1], out int value))
                return "user_id и value должны быть числами.";

            var targetUser = await _usersRepository.GetByID(userId);
            if (targetUser == null) return $"Пользователь с ID {userId} не найден.";

            await _usersRepository.UpdateWordCountToValue(userId, value);
            return $"WordCount для {targetUser.Username ?? targetUser.FirstName} = {value}";
        }
    }
}
