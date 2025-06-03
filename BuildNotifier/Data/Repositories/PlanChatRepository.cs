using BuildNotifier.Data.Context;
using BuildNotifier.Data.Models.DB;
using BuildNotifier.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BuildNotifier.Data.Repositories
{
    /// <summary>
    /// Реализация <see cref="IPlanChatRepository"/> для работы с БД через Entity Framework
    /// </summary>
    public class PlanChatRepository : IPlanChatRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="PlanChatRepository"/>.
        /// </summary>
        /// <param name="factory">Фабрика для создания контекста БД (AppDbContext). Позволяет управлять подключениями и жизненным циклом контекста.</param>
        public PlanChatRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        /// <inheritdoc cref="IPlanChatRepository.AddPlanChat(string, string)"/>
        public async Task<bool> AddPlanChatAsync(string planName, string chatId)
        {
            using var _db = await _factory.CreateDbContextAsync();

            bool linkExists = await _db.PlanChats
                    .AnyAsync(pc => pc.PlanName == planName && pc.ChatId == chatId);

            if (!linkExists)
            {
                _db.PlanChats.Add(new PlanChat
                {
                    PlanName = planName,
                    ChatId = chatId
                });
                return await _db.SaveChangesAsync() > 0;
            }
            return false;
        }

        /// <inheritdoc cref="IPlanChatRepository.DeleteAllPlansFromChat(string)"/>
        public async Task<bool> DeleteAllPlansFromChatAsync(string chatId)
        {
            using var _db = await _factory.CreateDbContextAsync();
            var planChats = await _db.PlanChats
                    .Where(pc => pc.ChatId == chatId)
                    .ToListAsync();

            if (planChats.Any())
            {
                _db.PlanChats.RemoveRange(planChats);
                return await _db.SaveChangesAsync() > 0;
            }
            return false;
        }

        /// <inheritdoc cref="IPlanChatRepository.DeletePlanChat(string, string)"/>
        public async Task<bool> DeletePlanChatAsync(string planName, string chatId)
        {
            using var _db = await _factory.CreateDbContextAsync();
            var planChat = await _db.PlanChats
                .FirstOrDefaultAsync(pc => pc.PlanName == planName && pc.ChatId == chatId);

            if (planChat != null)
            {
                _db.PlanChats.Remove(planChat);
                return await _db.SaveChangesAsync() > 0;
            }
            return false;
        }

        /// <inheritdoc cref="IPlanChatRepository.GetChatIds(string)"/>
        public async Task<List<string>> GetChatIdsAsync(string planName)
        {
            using var _db = await _factory.CreateDbContextAsync();
            return await _db.PlanChats
                .Where(pc => pc.PlanName == planName)
                .Select(pc => pc.ChatId)
                .ToListAsync();
        }

        /// <inheritdoc cref="IPlanChatRepository.GetPlanNames(string)"/>
        public async Task<List<string>> GetPlanNamesAsync(string chatId)
        {
            using var _db = await _factory.CreateDbContextAsync();
            return await _db.PlanChats
                .Where(pc => pc.ChatId == chatId)
                .Select(pc => pc.PlanName)
                .ToListAsync();
        }
    }
}