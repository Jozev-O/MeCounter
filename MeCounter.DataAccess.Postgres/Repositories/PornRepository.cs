using MeCounter.DataAccess.Postgres.interfaces;
using MeCounter.DataAccess.Postgres.Models;
using Microsoft.EntityFrameworkCore;

namespace MeCounter.DataAccess.Postgres.Repositories
{
    public class PornRepository(AppDbContext appDbContext) : IRepository<Porn>
    {
        private readonly AppDbContext _appDbContext = appDbContext;
        #region Read
        public async Task<List<Porn>> GetAll() =>
            await _appDbContext.Porns
            .AsNoTracking()
            .ToListAsync();

        public async Task<Porn?> GetByID(long id) =>
            await _appDbContext.Porns
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        #endregion
        #region Create
        public async Task Add(Porn porn)
        {
            await _appDbContext.Porns.AddAsync(porn);
            await _appDbContext.SaveChangesAsync();
        }
        public async Task Add(string porn)
        {
            var pornres = new Porn()
            {
                Url = new Uri(porn)
            };
            await _appDbContext.Porns.AddAsync(pornres);
            await _appDbContext.SaveChangesAsync();
        }
        public async Task Add(Uri uri)
        {
            var porn = new Porn()
            {
                Url = uri
            };
            await _appDbContext.Porns.AddAsync(porn);
            await _appDbContext.SaveChangesAsync();
        }
        #endregion
        #region Update
        public async Task Update(Porn porn) =>
            await _appDbContext.Porns
                .Where(p => p.Id == porn.Id)
                .ExecuteUpdateAsync(u => u.SetProperty(x => x.Url, porn.Url));

        #endregion
        #region Delete
        public async Task<int> Delete(Porn porn) =>
            await _appDbContext.Porns
            .Where(p => p.Id == porn.Id)
            .ExecuteDeleteAsync();

        public async Task<int> Delete(long id) =>
            await _appDbContext.Porns
            .Where(p => p.Id == id)
            .ExecuteDeleteAsync();

        #endregion
    }
}