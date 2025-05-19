using MeCounter.DataAccess.Postgres.interfaces;
using MeCounter.DataAccess.Postgres.Models;
using Microsoft.EntityFrameworkCore;

namespace MeCounter.DataAccess.Postgres.Repositories
{
    public class UsersRepository(AppDbContext appDbContext) : IRepository<User>
    {
        private readonly AppDbContext _appDbContext = appDbContext;

        #region Read
        public async Task<List<User>> GetAll()
        {
            return await _appDbContext.Users
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<User?> GetByID(long id)
        {
            return await _appDbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == id);
        }
        public async Task<int> GetUserWordCountAsync(long userId) =>
            (await _appDbContext.Users
            .FirstOrDefaultAsync(u => u.UserId == userId))?.WordCount ?? 0;

        public async Task<User?> GetByFirstName(string firstName) =>
            await _appDbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.FirstName == firstName);

        public async Task<User?> GetByLastName(string lastName) =>
            await _appDbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.LastName == lastName);

        public async Task<User?> GetByUserName(string userName) =>
            await _appDbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == userName);

        public async Task<ICollection<User>> GetByFilter(
            string? firstName = null,
            string? lastName = null,
            string? userName = null,
            int wordCount = -1)
        {
            var query = _appDbContext.Users.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(firstName)) query = query.Where(u => u.FirstName == firstName);
            if (!string.IsNullOrWhiteSpace(lastName)) query = query.Where(u => u.LastName == lastName);
            if (!string.IsNullOrWhiteSpace(userName)) query = query.Where(u => u.Username == userName);
            if (wordCount >= 0) query = query.Where(u => u.WordCount >= wordCount);
            return await query.ToListAsync();
        }

        public async Task<ICollection<User>> GetAdmins() =>
            await _appDbContext.Users.AsNoTracking()
            .Where(u => u.IsAdmin)
            .ToListAsync();

        public async Task<ICollection<User>> GetCounteble() =>
            await _appDbContext.Users.
            AsNoTracking()
            .Where(u => u.IsCounted)
            .ToListAsync();

        #endregion
        #region Ceate
        public async Task Add(User user)
        {
            await _appDbContext.Users
                .AddAsync(user);
            await _appDbContext.SaveChangesAsync();
        }
        #endregion
        #region Update
        public async Task Update(User user)
        {
            await _appDbContext.Users
                .Where(u => u.UserId == user.UserId)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(x => x.FirstName, user.FirstName)
                    .SetProperty(x => x.LastName, user.LastName)
                    .SetProperty(x => x.Username, user.Username)
                    .SetProperty(x => x.IsAdmin, user.IsAdmin)
                    .SetProperty(x => x.IsCounted, user.IsCounted)
                    .SetProperty(x => x.WordCount, user.WordCount));
        }
        public async Task UpdateIsAdmin(long id) =>
            await _appDbContext.Users
                .Where(u => u.UserId == id)
                .ExecuteUpdateAsync(u => u
                .SetProperty(x => x.IsAdmin, x => !x.IsAdmin));

        public async Task UpdateIsCountedFlag(long id) =>
            await _appDbContext.Users
                .Where(u => u.UserId == id)
                .ExecuteUpdateAsync(u => u
                .SetProperty(x => x.IsCounted, x => !x.IsCounted));

        public async Task UpdateWordCount(long id) =>
            await _appDbContext.Users
                .Where(u => u.UserId == id)
                .ExecuteUpdateAsync(u => u.
                SetProperty(x => x.WordCount, x => x.WordCount + 1));

        public async Task UpdateWordCountToValue(long userId, int wordCountValue) =>
            await _appDbContext.Users
                .Where(u => u.UserId == userId)
                .ExecuteUpdateAsync(u => u
                .SetProperty(x => x.WordCount, wordCountValue));
        #endregion
        #region Delete
        public async Task<int> Delete(User user)
        {
            return await _appDbContext.Users
                .Where(u => u.UserId == user.UserId)
                .ExecuteDeleteAsync();
        }
        public async Task<int> Delete(long id)
        {
            return await _appDbContext.Users
                .Where(u => u.UserId == id)
                .ExecuteDeleteAsync();
        }
        #endregion
    }
}