namespace ConfigAndSecurity.Domain
{
    public record ErrorResponse(string Message, string? RequestId = null);

}
