namespace SecurityGateway.Application.Dashboard.DTOs;

public class AttackTypeDto
{
    public required string Type { get; set; }
    public long Count { get; set; }
}
