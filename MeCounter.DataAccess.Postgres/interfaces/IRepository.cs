using MeCounter.DataAccess.Postgres.Models;

namespace MeCounter.DataAccess.Postgres.interfaces
{
    public interface IRepository<T>
    {
        public Task<List<T>> GetAll();
        public Task Add(T chat);
        public Task Update(T chat);
        public Task<int> Delete(T chat);
    }
}