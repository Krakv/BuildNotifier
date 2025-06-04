using BuildNotifier.Data.Models.DB;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BuildNotifier.Data.Context
{
    /// <summary>
    /// Контекст базы данных для работы с подписками чатов на планы сборок
    /// </summary>
    /// <remarks>
    /// Использует SQLite в качестве СУБД. Строка подключения берется из конфигурации.
    /// Содержит единственную сущность <see cref="PlanChat"/> с составным ключом (PlanName + ChatId).
    /// </remarks>
    /// <remarks>
    /// Инициализирует новый экземпляр контекста
    /// </remarks>
    /// <param name="configuration">Конфигурация приложения (для получения строки подключения)</param>
    public class AppDbContext(IConfiguration configuration) : DbContext
    {
        /// <summary>
        /// Набор данных для работы с подписками
        /// </summary>
        public DbSet<PlanChat> PlanChats { get; set; }

        private readonly IConfiguration _configuration = configuration;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = _configuration.GetConnectionString("SqliteConnection");
            optionsBuilder.UseSqlite(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PlanChat>()
                .HasKey(pc => new { pc.PlanName, pc.ChatId });

            modelBuilder.Entity<PlanChat>()
                .HasIndex(pc => pc.PlanName);

            modelBuilder.Entity<PlanChat>()
                .HasIndex(pc => pc.ChatId);
        }
    }
}
