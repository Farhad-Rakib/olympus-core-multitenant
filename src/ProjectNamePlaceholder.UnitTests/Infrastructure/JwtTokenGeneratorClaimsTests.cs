using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using ProjectNamePlaceholder.Application.Common.Interfaces.Security;
using ProjectNamePlaceholder.Application.Security;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Infrastructure.Authentication;

namespace ProjectNamePlaceholder.UnitTests.Infrastructure;

public class JwtTokenGeneratorClaimsTests
{
    private static readonly JwtTokenGenerator Generator = new(Options.Create(new JwtOptions
    {
        Issuer = "test",
        Audience = "test",
        SecretKey = "0123456789abcdef0123456789abcdef-test-key",
        ExpiryMinutes = 60,
    }));

    private static JwtSecurityToken Decode(string token) => new JwtSecurityTokenHandler().ReadJwtToken(token);

    [Fact]
    public void Default_token_carries_no_plan_or_impersonation_claims()
    {
        var token = Decode(Generator.GenerateAccessToken(new User("A", "a@x.test", "h"), ["users.read"]));

        Assert.DoesNotContain(token.Claims, c => c.Type == CustomClaims.PlanRequestsPerMinute);
        Assert.DoesNotContain(token.Claims, c => c.Type == CustomClaims.ImpersonatedBy);
    }

    [Fact]
    public void Plan_budget_and_impersonator_become_claims()
    {
        var token = Decode(Generator.GenerateAccessToken(new User("A", "a@x.test", "h"), [],
            new AccessTokenOptions(RequestsPerMinute: 1200, ImpersonatorUserId: 42)));

        Assert.Equal("1200", token.Claims.Single(c => c.Type == CustomClaims.PlanRequestsPerMinute).Value);
        Assert.Equal("42", token.Claims.Single(c => c.Type == CustomClaims.ImpersonatedBy).Value);
    }

    // The limiter treats "no claim" as "platform default"; a zero must never be emitted or the
    // int.TryParse path would size a bucket at 0 and lock the tenant out.
    [Fact]
    public void Zero_budget_is_not_emitted()
    {
        var token = Decode(Generator.GenerateAccessToken(new User("A", "a@x.test", "h"), [], new AccessTokenOptions(RequestsPerMinute: 0)));

        Assert.DoesNotContain(token.Claims, c => c.Type == CustomClaims.PlanRequestsPerMinute);
    }

    [Fact]
    public void Lifetime_override_shortens_expiry()
    {
        var token = Decode(Generator.GenerateAccessToken(new User("A", "a@x.test", "h"), [], new AccessTokenOptions(LifetimeMinutes: 5)));

        Assert.InRange(token.ValidTo, DateTime.UtcNow.AddMinutes(4), DateTime.UtcNow.AddMinutes(6));
    }
}
