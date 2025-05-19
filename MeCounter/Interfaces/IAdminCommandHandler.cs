namespace MeCounter.Interfaces
{
    public interface IAdminCommandHandler
    {
        string CommandName { get; }
        Task<string> HandleAsync(string[] args, long adminId, CancellationToken cancellationToken);
    }
}
