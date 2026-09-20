using System.ComponentModel.DataAnnotations;

namespace AspNetCoreDomainLab.Modules.Banking;

// Bab 12 — EF Core: entity banking mendukung account, transfer, ledger, dan risk review.
public sealed class BankingCustomer
{
    public Guid Id { get; set; }
    public string CustomerReference { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public List<BankAccount> Accounts { get; set; } = [];
}

public sealed class BankAccount
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string AccountReference { get; set; } = string.Empty;
    public string Currency { get; set; } = "SIM";
    public long BalanceMinor { get; set; }
    public string Status { get; set; } = "open";
    public int Version { get; set; }
    public BankingCustomer? Customer { get; set; }
    public List<AccountTransaction> Transactions { get; set; } = [];
}

public sealed class PaymentTransfer
{
    public Guid Id { get; set; }
    public Guid FromAccountId { get; set; }
    public Guid ToAccountId { get; set; }
    public long AmountMinor { get; set; }
    public string Currency { get; set; } = "SIM";
    public string Status { get; set; } = "completed-simulation";
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public BankAccount? FromAccount { get; set; }
    public BankAccount? ToAccount { get; set; }
    public List<FraudRiskReview> RiskReviews { get; set; } = [];
}

public sealed class AccountTransaction
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid? TransferId { get; set; }
    public long AmountMinor { get; set; }
    public string Direction { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public BankAccount? Account { get; set; }
    public PaymentTransfer? Transfer { get; set; }
}

public sealed class FraudRiskReview
{
    public Guid Id { get; set; }
    public Guid TransferId { get; set; }
    public string Status { get; set; } = "pending";
    public string Reason { get; set; } = string.Empty;
    public string? ReviewedBy { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? DecidedAtUtc { get; set; }
    public PaymentTransfer? Transfer { get; set; }
}

// Bab 8 — Validation: request records memvalidasi input shape sebelum aturan transfer dijalankan.
public sealed record CreateCustomerRequest(
    [param: Required, StringLength(40, MinimumLength = 3)] string CustomerReference,
    [param: Required, StringLength(120, MinimumLength = 2)] string DisplayName);

public sealed record CreateAccountRequest(
    [param: Required, StringLength(40, MinimumLength = 3)] string AccountReference,
    [param: Required, StringLength(3, MinimumLength = 3)] string Currency,
    [param: Range(0, long.MaxValue)] long InitialBalanceMinor);

public sealed record CreateTransferRequest(
    Guid FromAccountId,
    Guid ToAccountId,
    [param: Range(1, long.MaxValue)] long AmountMinor,
    [param: Required, StringLength(64, MinimumLength = 3)] string Currency,
    [param: Required, StringLength(80, MinimumLength = 3)] string IdempotencyKey);

public sealed record CreateRiskReviewRequest(
    Guid TransferId,
    [param: Required, StringLength(240, MinimumLength = 2)] string Reason);

public sealed record DecideRiskReviewRequest(
    [param: Required, StringLength(24, MinimumLength = 2)] string Decision,
    [param: Required, StringLength(120, MinimumLength = 2)] string Reviewer);

public sealed record CustomerResponse(
    Guid Id,
    string CustomerReference,
    string DisplayName,
    DateTimeOffset CreatedAtUtc);

public sealed record AccountResponse(
    Guid Id,
    Guid CustomerId,
    string AccountReference,
    string Currency,
    long BalanceMinor,
    string Status);

public sealed record TransferResponse(
    Guid Id,
    Guid FromAccountId,
    Guid ToAccountId,
    long AmountMinor,
    string Currency,
    string Status,
    string IdempotencyKey,
    DateTimeOffset CreatedAtUtc);

public sealed record AccountTransactionResponse(
    Guid Id,
    Guid AccountId,
    Guid? TransferId,
    long AmountMinor,
    string Direction,
    string Description,
    DateTimeOffset CreatedAtUtc);

public sealed record RiskReviewResponse(
    Guid Id,
    Guid TransferId,
    string Status,
    string Reason,
    string? ReviewedBy,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? DecidedAtUtc);
