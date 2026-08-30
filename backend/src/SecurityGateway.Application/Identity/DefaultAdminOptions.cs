namespace SecurityGateway.Application.Identity;

public sealed class DefaultAdminOptions
{
    public const string SectionName = "DefaultAdmin";

    public string Username { get; set; } = "admin";
    public string Email { get; set; } = "admin@toncom159.com";
    public string Password { get; set; } = "ChangeMeInProduction123!";
}
