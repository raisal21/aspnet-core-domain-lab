using Asp.Versioning;
using AspNetCoreDomainLab.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AspNetCoreDomainLab.Modules.Banking;

// Bab 2/6 — Controller REST boundary untuk customer, account, transfer, statement, dan risk review.
// Bab 7 — URL-segment versioning menjaga contract banking eksplisit.
// Bab 11 — read, transfer, dan risk memakai policy authorization tersendiri.
// Bab 14/15 — provider host mengecualikan response banking dari compression;
// transfer diberi rate limit fixed-window.
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/banking")]
public sealed class BankingController(BankingService service) : ControllerBase
{
    [HttpPost("customers")]
    [Authorize(Policy = AuthPolicies.BankingRead)]
    public async Task<ActionResult<CustomerResponse>> CreateCustomer(
        CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateCustomerAsync(request, cancellationToken);
        return Created($"{Request.PathBase}/api/v1/banking/customers/{response.Id}", response);
    }

    [HttpPost("customers/{customerId:guid}/accounts")]
    [Authorize(Policy = AuthPolicies.BankingRead)]
    public async Task<ActionResult<AccountResponse>> CreateAccount(
        Guid customerId,
        CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateAccountAsync(customerId, request, cancellationToken);
        return Created($"{Request.PathBase}/api/v1/banking/accounts/{response.Id}", response);
    }

    [HttpGet("accounts/{accountId:guid}")]
    [Authorize(Policy = AuthPolicies.BankingRead)]
    public async Task<ActionResult<AccountResponse>> GetAccount(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        return Ok(await service.GetAccountAsync(accountId, cancellationToken));
    }

    [HttpPost("transfers")]
    [Authorize(Policy = AuthPolicies.BankingTransfer)]
    [EnableRateLimiting("banking-transfer")]
    public async Task<ActionResult<TransferResponse>> CreateTransfer(
        CreateTransferRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateTransferAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("accounts/{accountId:guid}/transactions")]
    [Authorize(Policy = AuthPolicies.BankingRead)]
    public async Task<ActionResult<IReadOnlyList<AccountTransactionResponse>>> GetTransactions(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        return Ok(await service.GetTransactionsAsync(accountId, cancellationToken));
    }

    [HttpPost("risk-reviews")]
    [Authorize(Policy = AuthPolicies.BankingRisk)]
    public async Task<ActionResult<RiskReviewResponse>> CreateRiskReview(
        CreateRiskReviewRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateRiskReviewAsync(request, cancellationToken);
        return Created($"{Request.PathBase}/api/v1/banking/risk-reviews/{response.Id}", response);
    }

    [HttpPost("risk-reviews/{reviewId:guid}/decision")]
    [Authorize(Policy = AuthPolicies.BankingRisk)]
    public async Task<ActionResult<RiskReviewResponse>> DecideRiskReview(
        Guid reviewId,
        DecideRiskReviewRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await service.DecideRiskReviewAsync(reviewId, request, cancellationToken));
    }
}
