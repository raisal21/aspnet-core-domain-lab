using AspNetCoreDomainLab.Infrastructure.Api;
using AspNetCoreDomainLab.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreDomainLab.Modules.Banking;

// Bab 6 — service mengorkestrasi account, transfer, statement, dan risk-review workflow.
// Bab 9 — currency, balance, state, dan idempotency invariant menjadi domain errors.
// Bab 10 — structured logging mencatat transfer dan konflik tanpa membocorkan secret.
// Bab 12 — EF Core transaction dan concurrency token menjaga dua sisi ledger tetap konsisten.
public sealed class BankingService(
    DomainDbContext dbContext,
    ILogger<BankingService> logger)
{
    public async Task<CustomerResponse> CreateCustomerAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var reference = request.CustomerReference.Trim().ToUpperInvariant();
        if (await dbContext.BankingCustomers.AnyAsync(
                customer => customer.CustomerReference == reference,
                cancellationToken))
        {
            throw Rule(
                "customer_already_exists",
                "A banking customer with that reference already exists.",
                StatusCodes.Status409Conflict,
                reference);
        }

        var customer = new BankingCustomer
        {
            Id = Guid.NewGuid(),
            CustomerReference = reference,
            DisplayName = request.DisplayName.Trim(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        dbContext.BankingCustomers.Add(customer);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created synthetic banking customer {CustomerReference}", reference);
        return ToResponse(customer);
    }

    public async Task<AccountResponse> CreateAccountAsync(
        Guid customerId,
        CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureCustomerAsync(customerId, cancellationToken);
        var accountReference = request.AccountReference.Trim().ToUpperInvariant();
        if (await dbContext.BankAccounts.AnyAsync(
                account => account.AccountReference == accountReference,
                cancellationToken))
        {
            throw Rule(
                "account_already_exists",
                "A banking account with that reference already exists.",
                StatusCodes.Status409Conflict,
                accountReference);
        }

        var account = new BankAccount
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            AccountReference = accountReference,
            Currency = request.Currency.Trim().ToUpperInvariant(),
            BalanceMinor = request.InitialBalanceMinor,
            Status = "open",
        };
        dbContext.BankAccounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created synthetic account {AccountReference}", accountReference);
        return ToResponse(account);
    }

    public async Task<AccountResponse> GetAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var account = await GetAccountEntityAsync(accountId, cancellationToken);
        return ToResponse(account);
    }

    // Bab 12: satu database transaction menulis transfer, debit, credit, dan ledger history bersama.
    // Bab 9: idempotency key mengembalikan hasil lama saat client melakukan retry yang sama.
    public async Task<TransferResponse> CreateTransferAsync(
        CreateTransferRequest request,
        CancellationToken cancellationToken)
    {
        if (request.FromAccountId == request.ToAccountId)
        {
            throw Rule(
                "transfer_same_account",
                "A synthetic transfer must use two different accounts.",
                StatusCodes.Status422UnprocessableEntity);
        }

        var idempotencyKey = request.IdempotencyKey.Trim();
        var existing = await dbContext.PaymentTransfers.AsNoTracking()
            .SingleOrDefaultAsync(
                transfer => transfer.IdempotencyKey == idempotencyKey,
                cancellationToken);
        if (existing is not null)
        {
            if (existing.FromAccountId != request.FromAccountId ||
                existing.ToAccountId != request.ToAccountId ||
                existing.AmountMinor != request.AmountMinor)
            {
                throw Rule(
                    "transfer_idempotency_conflict",
                    "The idempotency key was already used with different transfer data.",
                    StatusCodes.Status409Conflict);
            }

            return ToResponse(existing);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var fromAccount = await GetAccountEntityAsync(request.FromAccountId, cancellationToken);
            var toAccount = await GetAccountEntityAsync(request.ToAccountId, cancellationToken);
            var currency = request.Currency.Trim().ToUpperInvariant();
            if (fromAccount.Currency != currency || toAccount.Currency != currency)
            {
                throw Rule(
                    "currency_mismatch",
                    "Both synthetic accounts must use the transfer currency.",
                    StatusCodes.Status422UnprocessableEntity,
                    fromAccount.AccountReference);
            }

            if (fromAccount.Status != "open" || toAccount.Status != "open")
            {
                throw Rule(
                    "account_not_open",
                    "Both synthetic accounts must be open for transfers.",
                    StatusCodes.Status409Conflict,
                    fromAccount.AccountReference);
            }

            if (fromAccount.BalanceMinor < request.AmountMinor)
            {
                throw Rule(
                    "insufficient_synthetic_balance",
                    "The source account does not have enough synthetic balance.",
                    StatusCodes.Status422UnprocessableEntity,
                    fromAccount.AccountReference);
            }

            var now = DateTimeOffset.UtcNow;
            var transfer = new PaymentTransfer
            {
                Id = Guid.NewGuid(),
                FromAccountId = fromAccount.Id,
                ToAccountId = toAccount.Id,
                AmountMinor = request.AmountMinor,
                Currency = currency,
                Status = "completed-simulation",
                IdempotencyKey = idempotencyKey,
                CreatedAtUtc = now,
            };
            fromAccount.BalanceMinor -= request.AmountMinor;
            fromAccount.Version++;
            toAccount.BalanceMinor += request.AmountMinor;
            toAccount.Version++;
            dbContext.PaymentTransfers.Add(transfer);
            dbContext.AccountTransactions.AddRange(
                new AccountTransaction
                {
                    Id = Guid.NewGuid(),
                    AccountId = fromAccount.Id,
                    TransferId = transfer.Id,
                    AmountMinor = request.AmountMinor,
                    Direction = "debit",
                    Description = $"Synthetic transfer to {toAccount.AccountReference}",
                    CreatedAtUtc = now,
                },
                new AccountTransaction
                {
                    Id = Guid.NewGuid(),
                    AccountId = toAccount.Id,
                    TransferId = transfer.Id,
                    AmountMinor = request.AmountMinor,
                    Direction = "credit",
                    Description = $"Synthetic transfer from {fromAccount.AccountReference}",
                    CreatedAtUtc = now,
                });
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation(
                "Completed synthetic transfer {TransferId} from {FromAccount} to {ToAccount}",
                transfer.Id,
                fromAccount.AccountReference,
                toAccount.AccountReference);
            return ToResponse(transfer);
        }
        // Bab 12: optimistic concurrency mengubah balance write bersamaan menjadi 409 yang dapat di-retry.
        catch (DbUpdateConcurrencyException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogWarning(exception, "Concurrent synthetic transfer conflict");
            throw Rule(
                "transfer_concurrency_conflict",
                "The synthetic transfer conflicted with another balance update. Retry the request with the same idempotency key.",
                StatusCodes.Status409Conflict,
                retryable: true);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<AccountTransactionResponse>> GetTransactionsAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        await EnsureAccountAsync(accountId, cancellationToken);
        return await dbContext.AccountTransactions.AsNoTracking()
            .Where(item => item.AccountId == accountId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(100)
            .Select(item => new AccountTransactionResponse(
                item.Id,
                item.AccountId,
                item.TransferId,
                item.AmountMinor,
                item.Direction,
                item.Description,
                item.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    // Bab 6/9: risk review adalah workflow terpisah dengan state pending -> approved/rejected.
    public async Task<RiskReviewResponse> CreateRiskReviewAsync(
        CreateRiskReviewRequest request,
        CancellationToken cancellationToken)
    {
        var transfer = await dbContext.PaymentTransfers
            .SingleOrDefaultAsync(item => item.Id == request.TransferId, cancellationToken)
            ?? throw Rule(
                "transfer_not_found",
                "The requested transfer was not found.",
                StatusCodes.Status404NotFound);
        if (await dbContext.FraudRiskReviews.AnyAsync(
                review => review.TransferId == transfer.Id && review.Status == "pending",
                cancellationToken))
        {
            throw Rule(
                "risk_review_already_pending",
                "A pending risk review already exists for this transfer.",
                StatusCodes.Status409Conflict);
        }

        var review = new FraudRiskReview
        {
            Id = Guid.NewGuid(),
            TransferId = transfer.Id,
            Status = "pending",
            Reason = request.Reason.Trim(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        dbContext.FraudRiskReviews.Add(review);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created risk review {RiskReviewId} for transfer {TransferId}", review.Id, transfer.Id);
        return ToResponse(review);
    }

    public async Task<RiskReviewResponse> DecideRiskReviewAsync(
        Guid reviewId,
        DecideRiskReviewRequest request,
        CancellationToken cancellationToken)
    {
        var review = await dbContext.FraudRiskReviews
            .SingleOrDefaultAsync(item => item.Id == reviewId, cancellationToken)
            ?? throw Rule(
                "risk_review_not_found",
                "The requested risk review was not found.",
                StatusCodes.Status404NotFound);
        if (review.Status != "pending")
        {
            throw Rule(
                "risk_review_already_decided",
                "The risk review has already been decided.",
                StatusCodes.Status409Conflict);
        }

        var decision = request.Decision.Trim().ToLowerInvariant();
        if (decision is not ("approved" or "rejected"))
        {
            throw Rule(
                "unsupported_risk_decision",
                "A risk decision must be approved or rejected.",
                StatusCodes.Status422UnprocessableEntity);
        }

        review.Status = decision;
        review.ReviewedBy = request.Reviewer.Trim();
        review.DecidedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Decided risk review {RiskReviewId} as {Decision}", reviewId, decision);
        return ToResponse(review);
    }

    private async Task EnsureCustomerAsync(Guid customerId, CancellationToken cancellationToken)
    {
        if (!await dbContext.BankingCustomers.AnyAsync(
                customer => customer.Id == customerId,
                cancellationToken))
        {
            throw Rule(
                "customer_not_found",
                "The requested banking customer was not found.",
                StatusCodes.Status404NotFound,
                customerId.ToString());
        }
    }

    private async Task EnsureAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        if (!await dbContext.BankAccounts.AnyAsync(
                account => account.Id == accountId,
                cancellationToken))
        {
            throw Rule(
                "account_not_found",
                "The requested banking account was not found.",
                StatusCodes.Status404NotFound,
                accountId.ToString());
        }
    }

    private async Task<BankAccount> GetAccountEntityAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        return await dbContext.BankAccounts
            .SingleOrDefaultAsync(account => account.Id == accountId, cancellationToken)
            ?? throw Rule(
                "account_not_found",
                "The requested banking account was not found.",
                StatusCodes.Status404NotFound,
                accountId.ToString());
    }

    private static CustomerResponse ToResponse(BankingCustomer customer) => new(
        customer.Id,
        customer.CustomerReference,
        customer.DisplayName,
        customer.CreatedAtUtc);

    private static AccountResponse ToResponse(BankAccount account) => new(
        account.Id,
        account.CustomerId,
        account.AccountReference,
        account.Currency,
        account.BalanceMinor,
        account.Status);

    private static TransferResponse ToResponse(PaymentTransfer transfer) => new(
        transfer.Id,
        transfer.FromAccountId,
        transfer.ToAccountId,
        transfer.AmountMinor,
        transfer.Currency,
        transfer.Status,
        transfer.IdempotencyKey,
        transfer.CreatedAtUtc);

    private static RiskReviewResponse ToResponse(FraudRiskReview review) => new(
        review.Id,
        review.TransferId,
        review.Status,
        review.Reason,
        review.ReviewedBy,
        review.CreatedAtUtc,
        review.DecidedAtUtc);

    private static DomainRuleException Rule(
        string code,
        string message,
        int statusCode,
        string? reference = null,
        bool retryable = false) => new(
        DomainArea.Banking,
        code,
        message,
        statusCode,
        reference,
        retryable);
}
