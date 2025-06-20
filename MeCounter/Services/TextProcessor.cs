using MeCounter.DataAccess.Postgres;
using MeCounter.DataAccess.Postgres.Repositories;
using MeCounter.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MeCounter.Services
{
    public class TextProcessor(UsersRepository usersRepository, PornRepository pornRepository, AppDbContext appDbContext) : ITextProcessor
    {
        private static readonly Regex KeyWordsRegex = new(@"(?i)[мmᴍℳΜḿṃɱᵯᶆ]\p{M}*[еeэёєɛæäëȩèéêēěėę℮ε]\p{M}*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly UsersRepository _usersRepository = usersRepository;
        private readonly PornRepository _pornRepository = pornRepository;
        private readonly AppDbContext _appDbContext = appDbContext;

        public async Task<string?> ProcessTextAsync(Message message, CancellationToken cancellationToken, ITelegramBotClient botClient)
        {
            var text = message.Text?.Trim().ToLower();
            if (string.IsNullOrEmpty(text))
                return null;

            text = RemoveInvisibleChars(text);

            // Реакция на "ахтяд бля"
            if (text == "ахмад бля")
                return "Сила баля!";

            // Реакция на "антон"
            if (text.Contains("антон"))
                return "Антон адыхает!";

            // Запрос порно
            if ((text.Contains("бот") && text.Contains("дай") && text.Contains("ключ")) ||
                (text.Contains("хочу") && text.Contains("порно")) ||
                (text.Contains("порн")))
            {
                if (message.Chat.Type != ChatType.Private)
                    return "Пиши в личку";

                int chatCount = await _appDbContext.Porns.CountAsync(cancellationToken);
                if (chatCount == 0)
                    return "не нашлось, попробуй еще";

                var ind = new Random().Next(1, chatCount + 1);
                var porn = await _pornRepository.GetByID(ind);

                var chats = await _appDbContext.Chats.ToListAsync();

                if (new Random().Next(1, 101) <= 10)
                {
                    foreach (var chat in chats)
                    {
                        await botClient.SendMessage(
                        chat.ChatId,
                        $"АХТУНГ!!!\n@{message.From.Username ?? message.From.FirstName} ИЩЕТ ПОРНУХУ. ПРЯМО ЩЯС!!!",
                        cancellationToken: cancellationToken);
                    }
                }
                return porn?.Url?.OriginalString ?? "не нашлось, попробуй еще";
            }


            // Обработка ключевых слов
            if (KeyWordsRegex.IsMatch(text))
            {
                var dbUser = await _usersRepository.GetByID(message.From.Id);
                if (dbUser == null)
                    return null;

                if (!dbUser.IsCounted)
                {
                    var words = new Dictionary<string, int>
                    {
                        ["Засчитаю на Ахмада."] = 30,
                        ["Босса не трогаем."] = 60,
                        ["Ну тебе можно, похуй."] = 100
                    };
                    var rand = new Random().Next(101);
                    return words.First(pair => rand <= pair.Value).Key;
                }

                await _usersRepository.UpdateWordCount(dbUser.UserId);
                dbUser = await _usersRepository.GetByID(message.From.Id);
                return $"Ооо, вот ты и попался\n(Счетчик: {dbUser.WordCount})";
            }

            return null;
        }
        public static string RemoveInvisibleChars(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            string result = new(input.Where(c =>
            {
                var category = char.GetUnicodeCategory(c);

                // Исключаем:
                // - Format (невидимые форматирующие символы)
                // - Control (управляющие символы)
                // - Punctuation (вся пунктуация)
                // - Symbol (математические, валютные и прочие символы)
                // - OtherSymbol (эмодзи и прочие специальные символы)
                return category != UnicodeCategory.Format &&
                       category != UnicodeCategory.Control &&
                       category != UnicodeCategory.OtherNotAssigned && // Эмодзи обычно здесь
                       (char.IsLetter(c) || char.IsDigit(c) || char.IsWhiteSpace(c));
            }).ToArray());

            return result;
        }
    }
}