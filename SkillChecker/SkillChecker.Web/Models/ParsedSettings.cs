namespace SkillChecker.Web.Models
{
    public class ParsedSettings
    {
        public string StartTime { get; set; } = "";
        public int TimeMinutes { get; set; }
        public bool Visible { get; set; } = true;
        public string DisplayTime
        {
            get
            {
                if (StartTime.Length > 0 && DateTime.TryParse(StartTime, out DateTime dt))
                {
                    return dt.ToString("dd.MM.yyyy HH:mm");
                }
                return "";
            }
        }
    }
}
