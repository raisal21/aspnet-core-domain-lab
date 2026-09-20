using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace AspNetCoreDomainLab.Infrastructure.Authentication;

// Bab 1/11 — authentication adalah dependency host yang dikonfigurasi sekali dan dipakai
// oleh semua controller melalui JWT bearer middleware serta authorization policy.
public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddDomainAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var jwtOptions = configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>()
            ?? throw new InvalidOperationException(
                "The Jwt configuration section is required.");
        jwtOptions.Validate();

        services.AddSingleton(jwtOptions);
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<IPasswordHasher<AuthUser>, PasswordHasher<AuthUser>>();
        services.AddSingleton<JwtTokenService>();
        services.AddScoped<ILocalAuthService, LocalAuthService>();

        // Bab 11: JWT validation memeriksa issuer, audience, signature, lifetime, name, dan role claims.
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.RequireHttpsMetadata = !environment.IsDevelopment();
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "name",
                    RoleClaimType = "role",
                };
            });

        // Bab 11: policy mengubah role domain menjadi authorization rule di endpoint.
        services.AddAuthorization(AuthPolicies.Configure);
        return services;
    }
}
