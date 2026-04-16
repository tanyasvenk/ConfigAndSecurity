using System.ComponentModel.DataAnnotations;

namespace ConfigAndSecurity.Config
{
    public class AppOptions
    {
        public const string SectionName = "AppSecurity";


        public AppMode Mode { get; set; }

        [MinLength(1, ErrorMessage = "Список доверенных источников не может быть пустым")]
        public List<string> TrustedOrigins { get; set; } = new();

        public RateLimitOptions RateLimit { get; set; } = new();
    }
}
