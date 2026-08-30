namespace SecurityGateway.Application.Identity;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
    bool IsConfigured { get; }
}
