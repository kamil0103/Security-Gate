namespace SecurityGateway.Application.Identity;

public sealed class AuthenticationException : Exception
{
    public AuthenticationException(string message) : base(message)
    {
    }
}
