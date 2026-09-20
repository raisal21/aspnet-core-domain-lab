using Microsoft.AspNetCore.Authorization;

namespace AspNetCoreDomainLab.Infrastructure.Authentication;

// Bab 11 — role adalah claim identity; policy di bawahnya menjadi contract authorization endpoint.
public static class AuthRoles
{
    public const string Administrator = "platform.admin";

    public const string HealthcareReader = "healthcare.read";
    public const string HealthcareWriter = "healthcare.write";

    public const string IndustrialOperator = "industrial.operator";
    public const string IndustrialMaintainer = "industrial.maintainer";

    public const string LogisticsReader = "logistics.read";
    public const string LogisticsDispatcher = "logistics.dispatcher";

    public const string BankingReader = "banking.read";
    public const string BankingTransfer = "banking.transfer";
    public const string BankingRiskReviewer = "banking.risk";
}

// Bab 11: policy domain-specific memberi platform.admin bypass tanpa mencampur role antar domain.
public static class AuthPolicies
{
    public const string HealthcareRead = "healthcare.read";
    public const string HealthcareWrite = "healthcare.write";
    public const string IndustrialOperate = "industrial.operate";
    public const string IndustrialMaintain = "industrial.maintain";
    public const string LogisticsRead = "logistics.read";
    public const string LogisticsDispatch = "logistics.dispatch";
    public const string BankingRead = "banking.read";
    public const string BankingTransfer = "banking.transfer";
    public const string BankingRisk = "banking.risk";

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(
            HealthcareRead,
            policy => policy.RequireAuthenticatedUser().RequireRole(
                AuthRoles.HealthcareReader,
                AuthRoles.Administrator));
        options.AddPolicy(
            HealthcareWrite,
            policy => policy.RequireAuthenticatedUser().RequireRole(
                AuthRoles.HealthcareWriter,
                AuthRoles.Administrator));
        options.AddPolicy(
            IndustrialOperate,
            policy => policy.RequireAuthenticatedUser().RequireRole(
                AuthRoles.IndustrialOperator,
                AuthRoles.Administrator));
        options.AddPolicy(
            IndustrialMaintain,
            policy => policy.RequireAuthenticatedUser().RequireRole(
                AuthRoles.IndustrialMaintainer,
                AuthRoles.Administrator));
        options.AddPolicy(
            LogisticsRead,
            policy => policy.RequireAuthenticatedUser().RequireRole(
                AuthRoles.LogisticsReader,
                AuthRoles.LogisticsDispatcher,
                AuthRoles.Administrator));
        options.AddPolicy(
            LogisticsDispatch,
            policy => policy.RequireAuthenticatedUser().RequireRole(
                AuthRoles.LogisticsDispatcher,
                AuthRoles.Administrator));
        options.AddPolicy(
            BankingRead,
            policy => policy.RequireAuthenticatedUser().RequireRole(
                AuthRoles.BankingReader,
                AuthRoles.BankingTransfer,
                AuthRoles.BankingRiskReviewer,
                AuthRoles.Administrator));
        options.AddPolicy(
            BankingTransfer,
            policy => policy.RequireAuthenticatedUser().RequireRole(
                AuthRoles.BankingTransfer,
                AuthRoles.Administrator));
        options.AddPolicy(
            BankingRisk,
            policy => policy.RequireAuthenticatedUser().RequireRole(
                AuthRoles.BankingRiskReviewer,
                AuthRoles.Administrator));
    }
}
