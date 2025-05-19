using MeCounter.DataAccess.Postgres.Repositories;
using MeCounter.Interfaces;
using MeCounter.Commands.Admin_commands;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace MeCounter.Commands
{
    public class AdminCommandHandler : ICommandHandler
    {
        private readonly ILogger<AdminCommandHandler> _logger;
        private readonly UsersRepository _usersRepository;
        private readonly Dictionary<string, IAdminCommandHandler> _commandHandlers;

        public AdminCommandHandler(
        ILogger<AdminCommandHandler> logger,
        UsersRepository usersRepository,
        SetCounterCommandHandler setCounterHandler,
        SetAdminCommandHandler setAdminHandler,
        SetCountedCommandHandler setCountedHandler,
        RestoreLinksCommandHandler restoreLinksHandler)
        {
            _logger = logger;
            _usersRepository = usersRepository;
            _commandHandlers = new Dictionary<string, IAdminCommandHandler>
        {
            { setCounterHandler.CommandName, setCounterHandler },
            { setAdminHandler.CommandName, setAdminHandler },
            { setCountedHandler.CommandName, setCountedHandler },
            { restoreLinksHandler.CommandName, restoreLinksHandler }
        };
        }


        public async Task<string> HandleAsync(Message message, CancellationToken cancellationToken)
        {
            if (message?.Text == null) return "Команда не распознана.";

            var user = await _usersRepository.GetByID(message.From.Id);
            if (user == null || !user.IsAdmin)
            {
                return "У вас нет прав для этой команды.";
            }

            var args = message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();
            if (args.Length == 0)
            {
                return "Укажите подкоманду, например: set_counter";
            }

            var commandName = args[0].ToLower();
            if (_commandHandlers.TryGetValue(commandName, out var handler))
            {
                return await handler.HandleAsync(args.Skip(1).ToArray(), message.From.Id, cancellationToken);
            }

            return "Неизвестная команда.";
        }
    }
}