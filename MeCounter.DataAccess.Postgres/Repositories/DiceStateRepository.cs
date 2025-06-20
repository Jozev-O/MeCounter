using MeCounter.DataAccess.Postgres.interfaces;
using MeCounter.DataAccess.Postgres.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace MeCounter.DataAccess.Postgres.Repositories
{
    public class DiceStateRepository : IRepository<DiceState>
    {
        private readonly AppDbContext _appDbContext;

        public DiceStateRepository(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<DiceState?> GetByID(long chatId) =>
            await _appDbContext.DiceStates.AsNoTracking()
                .FirstOrDefaultAsync(ds => ds.ChatId == chatId);

        public async Task Add(DiceState entity)
        {
            await _appDbContext.DiceStates.AddAsync(entity);
            await _appDbContext.SaveChangesAsync();
        }

        public async Task Update(DiceState entity)
        {
            await _appDbContext.DiceStates
                .Where(ds => ds.ChatId == entity.ChatId)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(x => x.LastValue, entity.LastValue)
                    .SetProperty(x => x.LastUser, entity.LastUser)
                    .SetProperty(x => x.LastMoniker, entity.LastMoniker));
        }

        public async Task<int> Delete(DiceState entity) =>
            await _appDbContext.DiceStates
                .Where(ds => ds.ChatId == entity.ChatId)
                .ExecuteDeleteAsync();

        public async Task<int> Delete(long chatId) =>
            await _appDbContext.DiceStates
                .Where(ds => ds.ChatId == chatId)
                .ExecuteDeleteAsync();

        public Task<List<DiceState>> GetAll() =>
            _appDbContext.DiceStates.AsNoTracking().ToListAsync();
    }
}