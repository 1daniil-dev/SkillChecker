using SkillChecker.Common.Models;

namespace SkillChecker.Web.Models
{
    public class ResultListItem : TestResult
    {
        private string _fileName;

        public string FileName { get => _fileName; set => _fileName = value; }

        public ResultListItem() : base()
        {
            _fileName = "";
        }
    }
}
