using MeCounter.DataAccess.Postgres.Models;
using MeCounter.DataAccess.Postgres.Repositories;
using MeCounter.Interfaces;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MeCounter.Commands
{
    public class SaveVideoCommandHandler : ICommandHandler
    {
        private readonly ILogger<SaveVideoCommandHandler> _logger;
        private readonly VideoMetadataRepository _videoRepository;
        private readonly ITelegramBotClient _botClient;
        private readonly string _videoStoragePath = "/app/videos"; // Путь внутри контейнера
        public string CommandName => "/savevideo";

        public SaveVideoCommandHandler(
            ILogger<SaveVideoCommandHandler> logger,
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
            // Проверяем наличие видео
            if (message.Video == null)
            {
                return "Пожалуйста, прикрепите видео к команде /savevideo.";
            }

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
            var videoMetadata = new VideoMetadata { FilePath = filePath };
            await _videoRepository.Add(videoMetadata);

            _logger.LogInformation("Видео от пользователя {UserId} сохранено локально: {FilePath}", userId, filePath);
            return $"Видео успешно сохранено! ID: {videoMetadata.Id}. Используйте /sendvideo [id], чтобы отправить его.";
        }
    }
}