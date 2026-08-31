using Fluxora.Application.Common;
using Fluxora.Application.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fluxora.Api.Controllers;

[ApiController]
[Authorize(Policy = AppPolicies.FinanceAccess)]
[Route("api/payables/{payableId:guid}/installments/{installmentId:guid}/payments")]
public class PaymentsController(PaymentService paymentService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<PaymentDto>> Apply(
        Guid payableId, Guid installmentId, ApplyPaymentRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await paymentService.ApplyAsync(payableId, installmentId, request, idempotencyKey ?? string.Empty, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, payment);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
        catch (ConcurrencyConflictException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Type = "https://fluxora.dev/problems/concurrency-conflict",
            });
        }
        catch (ConflictException ex)
        {
            return Conflict(new ProblemDetails { Title = ex.Message, Status = StatusCodes.Status409Conflict });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Title = ex.Message, Status = StatusCodes.Status409Conflict });
        }
    }
}
