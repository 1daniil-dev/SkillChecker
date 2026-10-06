namespace SkillChecker.Data
{
    public static class DatabaseInitializer
    {
        private static readonly object _lock = new object();

        public static void EnsureCreated(string dbPath)
        {
            lock (_lock)
            {
                using (AppDbContext db = new AppDbContext(dbPath))
                {
                    db.Database.EnsureCreated();
                }
            }
        }
    }
}
