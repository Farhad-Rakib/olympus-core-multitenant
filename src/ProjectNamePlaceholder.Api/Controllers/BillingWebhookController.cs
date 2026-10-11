using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using ProjectNamePlaceholder.Api.Middleware;
using ProjectNamePlaceholder.Application.Billing;
using ProjectNamePlaceholder.Application.Common.Configuration;

namespace ProjectNamePlaceholder.Api.Controllers;

// The payment provider calls this, so it cannot carry a JWT and cannot resolve a tenant from the
// host: it is authenticated by SIGNATURE alone, verified inside IPaymentGateway.ParseWebhookAsync.
// [SkipTenantResolution] is therefore required, and the tenant comes from the verified payload --
// never from a header a caller could set.
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/billing/webhook")]
[AllowAnonymous]
[SkipTenantResolution]
public sealed class BillingWebhookController : ControllerBase
{
    private readonly IPaymentGateway _paymentGateway;
    private readonly IBillingEventProcessor _billingEventProcessor;
    private readonly PaymentOptions _paymentOptions;
    private readonly ILogger<BillingWebhookController> _logger;

    public BillingWebhookController(
        IPaymentGateway paymentGateway,
        IBillingEventProcessor billingEventProcessor,
        IOptions<PaymentOptions> paymentOptions,
        ILogger<BillingWebhookController> logger)
    {
        _paymentGateway = paymentGateway;
        _billingEventProcessor = billingEventProcessor;
        _paymentOptions = paymentOptions.Value;
        _logger = logger;
    }

    [HttpPost]
    [DisableRateLimiting]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        if (!_paymentOptions.Enabled)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, "Billing is not enabled.");
        }

        // Read the RAW body: signature verification hashes the exact bytes sent, so anything that
        // re-serialises the payload (model binding) would invalidate the signature.
        string payload;
        using (var reader = new StreamReader(Request.Body))
        {
            payload = await reader.ReadToEndAsync(cancellationToken);
        }

        var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);

        BillingEvent? billingEvent;
        try
        {
            billingEvent = await _paymentGateway.ParseWebhookAsync(payload, headers, cancellationToken);
        }
        catch (Exception ex)
        {
            // A failed signature is an attacker or a misconfiguration, never a transient fault --
            // 400 so the provider stops retrying. The payload is deliberately not logged.
            _logger.LogWarning(ex, "Rejected a billing webhook whose signature did not verify.");
            return BadRequest("Invalid webhook signature.");
        }

        // Authentic but not an event we act on. 200, or the provider retries it forever.
        if (billingEvent is null)
        {
            return Ok();
        }

        var result = await _billingEventProcessor.ProcessAsync(billingEvent, cancellationToken);

        if (result is BillingEventResult.UnknownTenant or BillingEventResult.UnknownPlan)
        {
            _logger.LogError(
                "Billing event {ExternalEventId} could not be applied: {Result}.",
                billingEvent.ExternalEventId, result);
        }

        // 200 for every outcome above -- including the unknown-tenant/plan cases, which retrying
        // cannot fix and which would otherwise have the provider redeliver indefinitely.
        return Ok(new { result = result.ToString() });
    }
}
