using System.Text.Json.Serialization;

namespace SkillChecker.Web.Models
{
    public class ErrorResult
    {
        private string _error;

        [JsonPropertyName("error")]
        public string Error { get => _error; set => _error = value; }

        public ErrorResult()
        {
            _error = "";
        }
    }
}
