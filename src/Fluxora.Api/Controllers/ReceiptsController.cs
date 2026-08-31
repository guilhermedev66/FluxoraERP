using Fluxora.Application.Common;
using Fluxora.Application.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fluxora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/receivables/{receivableId:guid}/installments/{installmentId:guid}/receipts")]
public class ReceiptsController(ReceiptService receiptService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ReceiptDto>> Apply(
        Guid receivableId, Guid installmentId, ApplyReceiptRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        try
        {
            var receipt = await receiptService.ApplyAsync(receivableId, installmentId, request, idempotencyKey ?? string.Empty, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, receipt);
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
