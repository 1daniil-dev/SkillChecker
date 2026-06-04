using System.Text.Json.Serialization;

namespace SkillChecker.Web.Models
{
    public class OperationResult
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
    }
}
