using System.Text.Json.Serialization;

namespace SkillChecker.Web.Models
{
    public class AuthState
    {
        [JsonPropertyName("setup")]
        public bool Setup { get; set; }
    }
}
