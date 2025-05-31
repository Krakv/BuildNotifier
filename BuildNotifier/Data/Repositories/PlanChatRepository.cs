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

        public PlanChatRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public bool AddPlanChat(string planName, string chatId)
        {
            using var _db = _factory.CreateDbContext();

            bool linkExists = _db.PlanChats
                    .Any(pc => pc.PlanName == planName && pc.ChatId == chatId);

            if (!linkExists)
            {
                _db.PlanChats.Add(new PlanChat
                {
                    PlanName = planName,
                    ChatId = chatId
                });
                return _db.SaveChanges() > 0;
            }
            return false;
        }

        public bool DeleteAllPlansFromChat(string chatId)
        {
            using var _db = _factory.CreateDbContext();
            var planChats = _db.PlanChats
                    .Where(pc => pc.ChatId == chatId)
                    .ToList();

            if (planChats.Any())
            {
                _db.PlanChats.RemoveRange(planChats);
                return _db.SaveChanges() > 0;
            }
            return false;
        }

        public bool DeletePlanChat(string planName, string chatId)
        {
            using var _db = _factory.CreateDbContext();
            var planChat = _db.PlanChats
                .FirstOrDefault(pc => pc.PlanName == planName && pc.ChatId == chatId);

            if (planChat != null)
            {
                _db.PlanChats.Remove(planChat);
                return _db.SaveChanges() > 0;
            }
            return false;
        }

        public List<string> GetChatIds(string planName)
        {
            using var _db = _factory.CreateDbContext();
            return _db.PlanChats
                .Where(pc => pc.PlanName == planName)
                .Select(pc => pc.ChatId)
                .ToList();
        }

        public List<string> GetPlanNames(string chatId)
        {
            using var _db = _factory.CreateDbContext();
            return _db.PlanChats
                .Where(pc => pc.ChatId == chatId)
                .Select(pc => pc.PlanName)
                .ToList();
        }
    }
}