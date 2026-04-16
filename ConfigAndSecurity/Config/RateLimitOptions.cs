namespace ConfigAndSecurity.Config
{
    public class RateLimitOptions
    {
        public int GlobalPermitLimit { get; set; } = 100;
        public int StrictPermitLimit { get; set; } = 10;
        public int WindowMinutes { get; set; } = 1;
    }
}
