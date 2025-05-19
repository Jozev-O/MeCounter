using MeCounter.DataAccess.Postgres;
using MeCounter.DataAccess.Postgres.Repositories;
using MeCounter.Interfaces;

public class RestoreLinksCommandHandler : IAdminCommandHandler
{
    private readonly ChatsRepository _chatsRepository;
    private readonly UsersRepository _usersRepository;
    private readonly AppDbContext _appDbContext;

    public RestoreLinksCommandHandler(
        ChatsRepository chatsRepository,
        UsersRepository usersRepository,
        AppDbContext appDbContext)
    {
        _chatsRepository = chatsRepository;
        _usersRepository = usersRepository;
        _appDbContext = appDbContext;
    }

    public string CommandName => "restore_links";

    public async Task<string> HandleAsync(string[] args, long adminId, CancellationToken cancellationToken)
    {
        var chats = await _chatsRepository.GetAll();
        var users = await _usersRepository.GetAll();

        foreach (var chat in chats)
        {
            foreach (var user in users)
            {
                if (!chat.Users.Any(u => u.UserId == user.UserId))
                {
                    chat.Users.Add(user);
                }
            }
        }
        await _appDbContext.SaveChangesAsync();
        return $"Админ {adminId} восстановил связи чатов и пользователей";
    }
}