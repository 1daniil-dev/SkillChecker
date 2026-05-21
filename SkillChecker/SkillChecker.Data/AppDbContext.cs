using Microsoft.EntityFrameworkCore;

namespace SkillChecker.Data
{
    public class AppDbContext : DbContext
    {
        private string _dbPath;

        public DbSet<ResultEntity> Results { get; set; }

        public AppDbContext(string dbPath)
        {
            _dbPath = dbPath;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite("Data Source=" + _dbPath);
        }
    }
}
