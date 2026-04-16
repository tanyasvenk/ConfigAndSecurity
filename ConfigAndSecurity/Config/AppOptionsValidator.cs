using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.Extensions.Options;

namespace ConfigAndSecurity.Config
{
    public class AppOptionsValidator : IValidateOptions<AppOptions>
    {
        public ValidateOptionsResult Validate(string? name, AppOptions options)
        {
            var failures = new List<string>();

            foreach (var origin in options.TrustedOrigins)
            {
                if (!Uri.TryCreate(origin, UriKind.Absolute, out _))
                {
                    failures.Add($"Некорректный URL доверенного источника: {origin}");
                }
            }

            if (failures.Count > 0)
            {
                return ValidateOptionsResult.Fail(failures);
            }

            return ValidateOptionsResult.Success;
        }
    }
}
