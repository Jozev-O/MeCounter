using MeCounter.DataAccess.Postgres.Repositories;
using MeCounter.Interfaces;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MeCounter.Commands
{
    public class SendVideoCommandHandler(
        ILogger<SendVideoCommandHandler> logger,
        VideoMetadataRepository videoRepository,
        ITelegramBotClient botClient) : ICommandHandler
    {
        private readonly ILogger<SendVideoCommandHandler> _logger = logger;
        private readonly VideoMetadataRepository _videoRepository = videoRepository;
        private readonly ITelegramBotClient _botClient = botClient;
        public string CommandName => "/sendvideo";

        public async Task<string> HandleAsync(Message message, CancellationToken cancellationToken)
        {

            var args = message.Text?.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();
            if (args == null || args.Length != 1 || !long.TryParse(args[0], out long videoId))
                return "Используйте: /sendvideo [id]";

            var videoMetadata = await _videoRepository.GetByID(videoId);
            if (videoMetadata == null || !File.Exists(videoMetadata.FilePath))
                return "Видео с таким ID не найдено или файл отсутствует.";

            // Отправляем видео в чат
            using (var fileStream = new FileStream(videoMetadata.FilePath, FileMode.Open, FileAccess.Read))
            {
                await _botClient.SendVideo(
                    chatId: message.Chat.Id,
                    video: new InputFileStream(fileStream),
                    caption: $"Видео от @{message.From.Username ?? message.From.FirstName}",
                    cancellationToken: cancellationToken);
            }

            _logger.LogInformation("Видео {VideoId} отправлено в чат {ChatId}", videoId, message.Chat.Id);
            return "Видео отправлено!";
        }

    }
}