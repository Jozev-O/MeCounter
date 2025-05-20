using MeCounter.DataAccess.Postgres.Repositories;
using MeCounter.Interfaces;
using System.Threading;
using System.Threading.Tasks;

public class SetCountedCommandHandler(UsersRepository usersRepository) : IAdminCommandHandler
{
    private readonly UsersRepository _usersRepository = usersRepository;

    public string CommandName => "set_counted";

    public async Task<string> HandleAsync(string[] args, long adminId, CancellationToken cancellationToken)
    {
        if (args.Length < 1) return "Используйте: set_counted [user_id]";
        if (!long.TryParse(args[0], out long userId)) return "Некорректный user_id.";

        var targetUser = await _usersRepository.GetByID(userId);
        if (targetUser == null) return $"Пользователь с ID {userId} не найден.";

        await _usersRepository.UpdateIsCountedFlag(userId);
        targetUser = await _usersRepository.GetByID(userId);
        return $"IsCounted для {targetUser.Username ?? targetUser.FirstName} = {targetUser.IsCounted}";
    }
}