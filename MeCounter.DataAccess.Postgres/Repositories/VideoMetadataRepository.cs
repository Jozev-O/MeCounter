using MeCounter.DataAccess.Postgres.interfaces;
using MeCounter.DataAccess.Postgres.Models;
using Microsoft.EntityFrameworkCore;

namespace MeCounter.DataAccess.Postgres.Repositories
{
    public class VideoMetadataRepository(AppDbContext appDbContext) : IRepository<VideoMetadata>
    {
        private readonly AppDbContext _appDbContext = appDbContext;
        #region Read
        public async Task<List<VideoMetadata>> GetAll() => await _appDbContext.VideoMetadatas
            .AsNoTracking()
            .ToListAsync();

        public async Task<VideoMetadata?> GetByID(long id) => await _appDbContext.VideoMetadatas
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id);
        #endregion
        #region Create
        public async Task Add(VideoMetadata entity)
        {
            await _appDbContext.VideoMetadatas
                .AddAsync(entity);
            await _appDbContext.SaveChangesAsync();
        }
        #endregion
        #region Update
        public async Task Update(VideoMetadata entity) =>
            await _appDbContext.VideoMetadatas
             .Where(v => v.Id == entity.Id)
             .ExecuteUpdateAsync(u => u.SetProperty(x => x, entity));
        #endregion
        #region Delete
        public async Task<int> Delete(VideoMetadata entity) =>
            await _appDbContext.VideoMetadatas
            .Where(v => v.Id == entity.Id)
            .ExecuteDeleteAsync();

        public async Task<int> Delete(long id) =>
            await _appDbContext.VideoMetadatas
            .Where(v => v.Id == id)
            .ExecuteDeleteAsync();
        #endregion
    }
}