using System.Text.Json.Serialization;

namespace SkillChecker.Web.Models
{
    public class ErrorResult
    {
        [JsonPropertyName("error")]
        public string Error { get; set; } = "";
    }
}
