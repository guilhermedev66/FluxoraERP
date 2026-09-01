using Fluxora.Application.Common;
using Fluxora.Application.Purchasing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fluxora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/purchase-orders")]
public class PurchaseOrdersController(PurchaseOrderService purchaseOrderService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PurchaseOrderDto>>> List(
        [FromQuery] Guid? supplierId, [FromQuery] string? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        return Ok(await purchaseOrderService.ListAsync(supplierId, status, page, pageSize, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseOrderDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await purchaseOrderService.GetByIdAsync(id, cancellationToken));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<ActionResult<PurchaseOrderDto>> Create(CreatePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await purchaseOrderService.CreateDraftAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (ConflictException ex)
        {
            return Conflict(new ProblemDetails { Title = ex.Message, Status = StatusCodes.Status409Conflict });
        }
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult<PurchaseOrderDto>> AddLine(Guid id, AddPurchaseOrderLineRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await purchaseOrderService.AddLineAsync(id, request, cancellationToken));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (ConcurrencyConflictException ex)
        {
            return Conflict(new ProblemDetails { Title = ex.Message, Status = StatusCodes.Status409Conflict });
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Title = ex.Message, Status = StatusCodes.Status409Conflict });
        }
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<PurchaseOrderDto>> Confirm(Guid id, ConfirmPurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await purchaseOrderService.ConfirmAsync(id, request, cancellationToken));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (ConcurrencyConflictException ex)
        {
            return Conflict(new ProblemDetails { Title = ex.Message, Status = StatusCodes.Status409Conflict });
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Title = ex.Message, Status = StatusCodes.Status409Conflict });
        }
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<PurchaseOrderDto>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await purchaseOrderService.CancelAsync(id, cancellationToken));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (ConcurrencyConflictException ex)
        {
            return Conflict(new ProblemDetails { Title = ex.Message, Status = StatusCodes.Status409Conflict });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Title = ex.Message, Status = StatusCodes.Status409Conflict });
        }
    }
}
