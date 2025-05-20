using MeCounter.DataAccess.Postgres.Repositories;
using MeCounter.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace MeCounter.Commands
{
    public class NoSchetchikCommandHandler(UsersRepository usersRepository) : ICommandHandler
    {
        private readonly UsersRepository _usersRepository = usersRepository;

        public async Task<string> HandleAsync(Message message, CancellationToken cancellationToken)
        {
            var user = await _usersRepository.GetByID(message.From.Id);
            if (user == null)
                return "Ошибка: Пользователь не найден.";

            if (!user.IsAdmin)
            {
                var words = new Dictionary<string, int>
                {
                    [$"Ооо, вот ты и попался(Счетчик: {new Random().Next(10001)})"] = 4,
                    ["Иди гуляй."] = 5,
                    ["Отключить? Чай поставь"] = 8,
                    ["Выключить хочешь? А может в Tekken?"] = 10,
                    ["Ну ты и шустрый."] = 24,
                    ["Куда полез? Иди травку кушай"] = 49
                };
                var rand = new Random().Next(101);
                return words.OrderBy(pair => pair.Value)
                           .FirstOrDefault(pair => rand <= pair.Value).Key
                           ?? words.Last().Key;
            }

            var adminWords = new Dictionary<string, int>
            {
                ["Раз уж вы хотите поиграть в эту игру..."] = 4,
                ["Запретов нет — делайте, что хотите."] = 5,
                ["Без вопросов, командир."] = 8,
                ["Как скажете, босс."] = 10,
                ["Как пожелаете, шеф."] = 24,
                ["Сделано."] = 49
            };
            var adminRand = new Random().Next(101);
            await _usersRepository.UpdateIsCountedFlag(user.UserId);
            var reply = adminWords.OrderBy(pair => pair.Value)
                                 .FirstOrDefault(pair => adminRand <= pair.Value).Key
                                 ?? adminWords.Last().Key;
            reply += $"\nСчетчик {(user.IsCounted ? "включен" : "выключен")}.";
            return reply;
        }
    }
}