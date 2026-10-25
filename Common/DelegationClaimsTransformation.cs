// File: Common/DelegationClaimsTransformation.cs
using System.Security.Claims;
using aractakipsys.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;

namespace aractakipsys.Common;

public class DelegationClaimsTransformation : IClaimsTransformation
{
    private readonly IRoleDelegationService _roleDelegationService;
    private readonly ILogger<DelegationClaimsTransformation> _logger;

    public DelegationClaimsTransformation(
        IRoleDelegationService roleDelegationService,
        ILogger<DelegationClaimsTransformation> logger)
    {
        _roleDelegationService = roleDelegationService;
        _logger = logger;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity == null || !principal.Identity.IsAuthenticated)
        {
            return principal;
        }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return principal;
        }

        // DÜZELTME 1: Claims transformation idempotent olsun (aynı claim iki kez eklenmesin).
        // Eğer zaten bu request'te eklenmişse kontrol et
        var identity = principal.Identity as ClaimsIdentity;
        if (identity == null)
        {
            return principal;
        }

        var activeDelegation = await _roleDelegationService.GetActiveDelegationForUserAsync(userId);
        if (activeDelegation != null)
        {
            // Kullanici rolü ekle (idempotent)
            if (!principal.IsInRole(AppRoles.Kullanici))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, AppRoles.Kullanici));
                _logger.LogInformation("Vekil kullanıcıya dinamik Kullanici (Şef) rolü verildi. UserId: {UserId}, DelegationId: {DelegationId}",
                    userId, activeDelegation.Id);
            }

            // DelegatorUserId claim (idempotent)
            if (!identity.HasClaim(c => c.Type == "DelegatorUserId"))
            {
                identity.AddClaim(new Claim("DelegatorUserId", activeDelegation.DelegatorUserId));
            }

            // DelegationId claim (idempotent)
            if (!identity.HasClaim(c => c.Type == "DelegationId"))
            {
                identity.AddClaim(new Claim("DelegationId", activeDelegation.Id.ToString()));
            }

            // DelegatedRole claim (idempotent)
            if (!identity.HasClaim(c => c.Type == "DelegatedRole"))
            {
                identity.AddClaim(new Claim("DelegatedRole", activeDelegation.DelegatedRole));
            }
        }
        else
        {
            // Eğer vekâlet sonlandırılmışsa veya bitmişse, claim'ler varsa temizle (idempotent koruma)
            var existingOnBehalf = identity.FindFirst("DelegatorUserId");
            if (existingOnBehalf != null)
            {
                identity.RemoveClaim(existingOnBehalf);
            }

            var existingDelId = identity.FindFirst("DelegationId");
            if (existingDelId != null)
            {
                identity.RemoveClaim(existingDelId);
            }

            var existingDelRole = identity.FindFirst("DelegatedRole");
            if (existingDelRole != null)
            {
                identity.RemoveClaim(existingDelRole);
            }
        }

        return principal;
    }
}
