using SecurityGateway.Application.Blocking.DTOs;

namespace SecurityGateway.Application.Blocking;

public interface IAutomaticBlockingService
{
    Task<BlockResultDto?> CheckAndBlockAsync(string ipAddress, int? threatScore = null, CancellationToken cancellationToken = default);
    Task<BlockResultDto> BlockAsync(string ipAddress, int? durationMinutes = null, string? reason = null, CancellationToken cancellationToken = default);
    Task UnblockAsync(string ipAddress, CancellationToken cancellationToken = default);
    Task<bool> IsBlockedAsync(string ipAddress, CancellationToken cancellationToken = default);
}
