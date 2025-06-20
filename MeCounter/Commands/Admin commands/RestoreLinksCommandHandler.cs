using MeCounter.DataAccess.Postgres;
using MeCounter.DataAccess.Postgres.Repositories;
using MeCounter.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class RestoreLinksCommandHandler : IAdminCommandHandler
{
    private readonly ChatsRepository _chatsRepository;
    private readonly UsersRepository _usersRepository;
    private readonly AppDbContext _appDbContext;
    private readonly ILogger<RestoreLinksCommandHandler> _logger;

    public RestoreLinksCommandHandler(
        ChatsRepository chatsRepository,
        UsersRepository usersRepository,
        AppDbContext appDbContext,
        ILogger<RestoreLinksCommandHandler> logger)
    {
        _chatsRepository = chatsRepository;
        _usersRepository = usersRepository;
        _appDbContext = appDbContext;
        _logger = logger;
    }

    public string CommandName => "restore_links";

    public async Task<string> HandleAsync(string[] args, long adminId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Начато восстановление связей чатов и пользователей администратором {AdminId}", adminId);

        var chats = await _chatsRepository.GetAll();
        var users = await _usersRepository.GetAll();

        int addedLinks = 0;
        foreach (var chat in chats)
        {
            foreach (var user in users)
            {
                if (!chat.Users.Any(u => u.UserId == user.UserId))
                {
                    chat.Users.Add(user);
                    addedLinks++;
                }
            }
        }

        if (addedLinks > 0)
        {
            await _appDbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Добавлено {AddedLinks} новых связей чатов и пользователей", addedLinks);
            return $"Админ {adminId} восстановил {addedLinks} связей чатов и пользователей.";
        }

        _logger.LogInformation("Новые связи не добавлены, все пользователи уже связаны с чатами");
        return "Все пользователи уже связаны с чатами.";
    }
}