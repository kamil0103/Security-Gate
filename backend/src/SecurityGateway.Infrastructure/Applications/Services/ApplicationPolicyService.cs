using SecurityGateway.Application.Applications;
using SecurityGateway.Application.Applications.DTOs;
using SecurityGateway.Application.Applications.Models;
using SecurityGateway.Application.Identity;
using ApplicationEntity = SecurityGateway.Domain.Applications.Application;
using ApplicationPolicyEntity = SecurityGateway.Domain.Applications.ApplicationPolicy;

namespace SecurityGateway.Infrastructure.Applications.Services;

public sealed class ApplicationPolicyService : IApplicationPolicyService
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IApplicationPolicyRepository _policyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ApplicationPolicyService(
        IApplicationRepository applicationRepository,
        IApplicationPolicyRepository policyRepository,
        IUnitOfWork unitOfWork)
    {
        _applicationRepository = applicationRepository;
        _policyRepository = policyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ApplicationDto>> GetApplicationsAsync(CancellationToken cancellationToken = default)
    {
        var applications = await _applicationRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return applications.Select(a => MapApplication(a)).ToList().AsReadOnly();
    }

    public async Task<ApplicationDto?> GetApplicationByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var application = await _applicationRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return application is null ? null : MapApplication(application);
    }

    public async Task<ApplicationDto?> GetApplicationByDomainAsync(string domain, CancellationToken cancellationToken = default)
    {
        var application = await _applicationRepository.GetByDomainAsync(domain, cancellationToken).ConfigureAwait(false);
        return application is null ? null : MapApplication(application);
    }

    public async Task<ApplicationDto> CreateApplicationAsync(CreateApplicationRequest request, CancellationToken cancellationToken = default)
    {
        ValidateApplicationRequest(request);

        var existing = await _applicationRepository.GetByDomainAsync(request.Domain, cancellationToken).ConfigureAwait(false);

        if (existing is not null)
        {
            throw new InvalidOperationException("An application with this domain already exists.");
        }

        var application = new ApplicationEntity
        {
            Name = request.Name,
            Domain = request.Domain,
            UpstreamUrl = request.UpstreamUrl
        };

        await _applicationRepository.AddAsync(application, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var defaultPolicy = new ApplicationPolicyEntity
        {
            ApplicationId = application.Id,
            RequireAuthentication = true
        };

        await _policyRepository.AddAsync(defaultPolicy, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapApplication(application, defaultPolicy);
    }

    public async Task<ApplicationDto> UpdateApplicationAsync(Guid id, UpdateApplicationRequest request, CancellationToken cancellationToken = default)
    {
        var application = await _applicationRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Application not found.");

        if (application.Domain != request.Domain)
        {
            var existing = await _applicationRepository.GetByDomainAsync(request.Domain, cancellationToken).ConfigureAwait(false);

            if (existing is not null)
            {
                throw new InvalidOperationException("An application with this domain already exists.");
            }
        }

        application.Name = request.Name;
        application.Domain = request.Domain;
        application.UpstreamUrl = request.UpstreamUrl;
        application.IsEnabled = request.IsEnabled;

        await _applicationRepository.UpdateAsync(application, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapApplication(application);
    }

    public async Task DeleteApplicationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var application = await _applicationRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Application not found.");

        await _applicationRepository.DeleteAsync(application, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<ApplicationPolicyDto?> GetPolicyAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        var policy = await _policyRepository.GetByApplicationIdAsync(applicationId, cancellationToken).ConfigureAwait(false);
        return policy is null ? null : MapPolicy(policy);
    }

    public async Task<ApplicationPolicyDto> UpdatePolicyAsync(Guid applicationId, UpdateApplicationPolicyRequest request, CancellationToken cancellationToken = default)
    {
        var application = await _applicationRepository.GetByIdAsync(applicationId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Application not found.");

        var policy = await _policyRepository.GetByApplicationIdAsync(applicationId, cancellationToken).ConfigureAwait(false);

        if (policy is null)
        {
            policy = new ApplicationPolicyEntity
            {
                ApplicationId = applicationId,
                RequireAuthentication = request.RequireAuthentication,
                AllowAnonymousFromTrustedNetworks = request.AllowAnonymousFromTrustedNetworks,
                AllowedCountries = request.AllowedCountries,
                BlockedCountries = request.BlockedCountries,
                AllowedIpAddresses = request.AllowedIpAddresses,
                BlockedIpAddresses = request.BlockedIpAddresses
            };

            await _policyRepository.AddAsync(policy, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            policy.RequireAuthentication = request.RequireAuthentication;
            policy.AllowAnonymousFromTrustedNetworks = request.AllowAnonymousFromTrustedNetworks;
            policy.AllowedCountries = request.AllowedCountries;
            policy.BlockedCountries = request.BlockedCountries;
            policy.AllowedIpAddresses = request.AllowedIpAddresses;
            policy.BlockedIpAddresses = request.BlockedIpAddresses;

            await _policyRepository.UpdateAsync(policy, cancellationToken).ConfigureAwait(false);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapPolicy(policy);
    }

    public async Task<ApplicationPolicyEvaluation> EvaluatePolicyAsync(Guid applicationId, string ipAddress, bool isAuthenticated, bool isIpTrusted, CancellationToken cancellationToken = default)
    {
        var application = await _applicationRepository.GetByIdAsync(applicationId, cancellationToken).ConfigureAwait(false);

        if (application is null)
        {
            return new ApplicationPolicyEvaluation
            {
                Allowed = false,
                Reason = "Application not found.",
                RequiresAuthentication = false,
                IsAuthenticated = isAuthenticated
            };
        }

        if (!application.IsEnabled)
        {
            return new ApplicationPolicyEvaluation
            {
                Allowed = false,
                Reason = "Application is disabled.",
                RequiresAuthentication = false,
                IsAuthenticated = isAuthenticated
            };
        }

        var policy = application.Policy ?? new ApplicationPolicyEntity();

        if (!string.IsNullOrWhiteSpace(policy.BlockedIpAddresses))
        {
            var blockedIps = ParseList(policy.BlockedIpAddresses);

            if (blockedIps.Contains(ipAddress))
            {
                return Deny("IP address is blocked by application policy.", policy.RequireAuthentication, isAuthenticated);
            }
        }

        if (!string.IsNullOrWhiteSpace(policy.AllowedIpAddresses))
        {
            var allowedIps = ParseList(policy.AllowedIpAddresses);

            if (!allowedIps.Contains(ipAddress))
            {
                return Deny("IP address is not in the application allowlist.", policy.RequireAuthentication, isAuthenticated);
            }
        }

        if (!string.IsNullOrWhiteSpace(policy.BlockedCountries))
        {
            // Country-based blocking requires GeoIP data; placeholder for future integration.
        }

        var requiresAuthentication = policy.RequireAuthentication && !(policy.AllowAnonymousFromTrustedNetworks && isIpTrusted);

        if (requiresAuthentication && !isAuthenticated)
        {
            return new ApplicationPolicyEvaluation
            {
                Allowed = false,
                Reason = "Authentication required.",
                RequiresAuthentication = true,
                IsAuthenticated = isAuthenticated
            };
        }

        return new ApplicationPolicyEvaluation
        {
            Allowed = true,
            Reason = null,
            RequiresAuthentication = requiresAuthentication,
            IsAuthenticated = isAuthenticated
        };
    }

    private static ApplicationPolicyEvaluation Deny(string reason, bool requiresAuthentication, bool isAuthenticated)
    {
        return new ApplicationPolicyEvaluation
        {
            Allowed = false,
            Reason = reason,
            RequiresAuthentication = requiresAuthentication,
            IsAuthenticated = isAuthenticated
        };
    }

    private static void ValidateApplicationRequest(CreateApplicationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Name is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Domain))
        {
            throw new ArgumentException("Domain is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.UpstreamUrl))
        {
            throw new ArgumentException("Upstream URL is required.", nameof(request));
        }
    }

    private static IReadOnlySet<string> ParseList(string value)
    {
        return value
            .Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static ApplicationDto MapApplication(ApplicationEntity application, ApplicationPolicyEntity? policy = null)
    {
        return new ApplicationDto
        {
            Id = application.Id,
            Name = application.Name,
            Domain = application.Domain,
            UpstreamUrl = application.UpstreamUrl,
            IsEnabled = application.IsEnabled,
            CreatedAt = application.CreatedAt,
            Policy = policy is not null ? MapPolicy(policy) : (application.Policy is not null ? MapPolicy(application.Policy) : null)
        };
    }

    private static ApplicationPolicyDto MapPolicy(ApplicationPolicyEntity policy)
    {
        return new ApplicationPolicyDto
        {
            Id = policy.Id,
            ApplicationId = policy.ApplicationId,
            RequireAuthentication = policy.RequireAuthentication,
            AllowAnonymousFromTrustedNetworks = policy.AllowAnonymousFromTrustedNetworks,
            AllowedCountries = policy.AllowedCountries,
            BlockedCountries = policy.BlockedCountries,
            AllowedIpAddresses = policy.AllowedIpAddresses,
            BlockedIpAddresses = policy.BlockedIpAddresses
        };
    }
}
