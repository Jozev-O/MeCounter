using MeCounter.Interfaces;
using System.Reflection;
using Telegram.Bot.Types;

namespace MeCounter.Commands
{
    public class VersionCommandHandler : ICommandHandler
    {
        public Task<string> HandleAsync(Message message, CancellationToken cancellationToken)
        {
            if (message == null)
            {
                return Task.FromResult("Сообщение не распознано.");
            }

            return Task.FromResult(
                Assembly
                    .GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                    .InformationalVersion
                    ?.ToString() ?? "Версия не найдена.");
        }
    }
}
