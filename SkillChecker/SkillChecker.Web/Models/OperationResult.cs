using System.Text.Json.Serialization;

namespace SkillChecker.Web.Models
{
    public class OperationResult
    {
        private bool _ok;
        private string _name;

        [JsonPropertyName("ok")]
        public bool Ok { get => _ok; set => _ok = value; }
        [JsonPropertyName("name")]
        public string Name { get => _name; set => _name = value; }

        public OperationResult()
        {
            _ok = false;
            _name = "";
        }
    }
}
