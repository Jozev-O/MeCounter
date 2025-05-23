using MeCounter.DataAccess.Postgres.Models;
using MeCounter.DataAccess.Postgres.Repositories;
using MeCounter.Interfaces;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MeCounter.Commands
{
    public class VideoCommandHandler : ICommandHandler
    {
        private readonly ILogger<VideoCommandHandler> _logger;
        private readonly VideoMetadataRepository _videoRepository;
        private readonly ITelegramBotClient _botClient;
        private readonly string _videoStoragePath = "/app/videos"; // Путь внутри контейнера

        public VideoCommandHandler(
            ILogger<VideoCommandHandler> logger,
            VideoMetadataRepository videoRepository,
            ITelegramBotClient botClient)
        {
            _logger = logger;
            _videoRepository = videoRepository;
            _botClient = botClient;
            Directory.CreateDirectory(_videoStoragePath); // Создаем директорию, если ее нет
        }

        public async Task<string> HandleAsync(Message message, CancellationToken cancellationToken)
        {
            if (message.Video == null)
                return "Пожалуйста, отправьте видео.";

            var userId = message.From.Id;
            var fileId = message.Video.FileId;

            // Скачиваем видео из Telegram
            var file = await _botClient.GetFile(fileId, cancellationToken);
            var fileName = $"{Guid.NewGuid()}.mp4";
            var filePath = Path.Combine(_videoStoragePath, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await _botClient.DownloadFile(file.FilePath, fileStream, cancellationToken);
            }

            // Сохраняем метаданные в базе
            var videoMetadata = new VideoMetadata {FilePath = filePath };
            await _videoRepository.Add(videoMetadata);

            _logger.LogInformation("Видео от пользователя {UserId} сохранено локально: {FilePath}", userId, filePath);
            return $"Видео успешно сохранено! ID: {videoMetadata.Id}. Используйте /sendvideo [id], чтобы отправить его.";
        }

        public async Task<string> SendVideoAsync(Message message, CancellationToken cancellationToken)
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
                    //caption: $"Видео от @{message.From.Username ?? message.From.FirstName}",
                    cancellationToken: cancellationToken);
            }

            _logger.LogInformation("Видео {VideoId} отправлено в чат {ChatId}", videoId, message.Chat.Id);
            return "";
        }
    }
}