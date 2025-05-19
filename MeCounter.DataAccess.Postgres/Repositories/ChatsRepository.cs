using MeCounter.DataAccess.Postgres.interfaces;
using MeCounter.DataAccess.Postgres.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace MeCounter.DataAccess.Postgres.Repositories
{
    public class ChatsRepository(AppDbContext appDbContext) : IRepository<Chat>
    {
        private readonly AppDbContext _appDbContext = appDbContext;
        #region Read
        public async Task<List<Chat>> GetAll() =>
            await _appDbContext.Chats
            .AsNoTracking()
            .ToListAsync();

        public async Task<Chat?> GetByID(long id) =>
            await _appDbContext.Chats.
            AsNoTracking()
            .Include(c => c.Users)
            .FirstOrDefaultAsync(c => c.ChatId == id);

        public async Task<List<User>> GetUsersByChatId(long chatId) =>
            await _appDbContext.Chats.AsNoTracking()
                .Where(c => c.ChatId == chatId)
                .SelectMany(c => c.Users)
                .ToListAsync();

        public async Task<Chat?> GetByTitle(string title) =>
            await _appDbContext.Chats
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Title == title);
        #endregion
        #region Create
        public async Task Add(Chat chat)
        {
            await _appDbContext.Chats.AddAsync(chat);
            await _appDbContext.SaveChangesAsync();
        }
        public async Task Add(long id, string title, ICollection<User> users)
        {
            var chat = new Chat()
            {
                ChatId = id,
                Title = title,
                Users = users
            };
            await _appDbContext.Chats.AddAsync(chat);
            await _appDbContext.SaveChangesAsync();
        }
        #endregion
        #region Update
        public async Task Update(Chat chat)
        {
            // Находим существующий чат в базе данных
            var existingChat = await _appDbContext.Chats
                .Include(c => c.Users) // Загружаем текущих пользователей
                .FirstOrDefaultAsync(c => c.ChatId == chat.ChatId);

            if (existingChat == null)
            {
                throw new Exception($"Чат с ID {chat.ChatId} не найден.");
            }

            // Обновляем простые свойства
            existingChat.Title = chat.Title;

            // Добавляем новых пользователей, если их еще нет
            foreach (var user in chat.Users)
            {
                if (!existingChat.Users.Any(u => u.UserId == user.UserId))
                {
                    existingChat.Users.Add(user);
                }
            }

            // Сохраняем изменения
            await _appDbContext.SaveChangesAsync();
        }
        public async Task Update(long id, string title, List<User> users)
        {
            // Находим существующий чат в базе данных
            var existingChat = await _appDbContext.Chats
                .Include(c => c.Users) // Загружаем текущих пользователей
                .FirstOrDefaultAsync(c => c.ChatId == id);

            if (existingChat == null)
            {
                throw new Exception($"Чат с ID {id} не найден.");
            }

            // Обновляем простые свойства
            existingChat.Title = title;

            // Добавляем новых пользователей, если их еще нет
            foreach (var user in users)
            {
                if (!existingChat.Users.Any(u => u.UserId == user.UserId))
                {
                    existingChat.Users.Add(user);
                }
            }

            // Сохраняем изменения
            await _appDbContext.SaveChangesAsync();
        }
        #endregion
        #region Delete
        public async Task<int> Delete(Chat chat) =>
            await _appDbContext.Chats
            .Where(c => c.ChatId == chat.ChatId)
            .ExecuteDeleteAsync();

        public async Task<int> Delete(long id) =>
            await _appDbContext.Chats
            .Where(c => c.ChatId == id)
            .ExecuteDeleteAsync();
        #endregion
    }
}