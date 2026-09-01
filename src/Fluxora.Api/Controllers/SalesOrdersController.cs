using Fluxora.Application.Common;
using Fluxora.Application.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fluxora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/sales-orders")]
public class SalesOrdersController(SalesOrderService salesOrderService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SalesOrderDto>>> List(
        [FromQuery] Guid? customerId, [FromQuery] string? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        return Ok(await salesOrderService.ListAsync(customerId, status, page, pageSize, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SalesOrderDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await salesOrderService.GetByIdAsync(id, cancellationToken));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<ActionResult<SalesOrderDto>> Create(CreateSalesOrderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await salesOrderService.CreateDraftAsync(request, cancellationToken);
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
    public async Task<ActionResult<SalesOrderDto>> AddLine(Guid id, AddSalesOrderLineRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await salesOrderService.AddLineAsync(id, request, cancellationToken));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (ConcurrencyConflictException ex)
        {
            return Conflict(new ProblemDetails { Title = ex.Message, Status = StatusCodes.Status409Conflict });
        }
        catch (ConflictException ex)
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

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<SalesOrderDto>> Approve(Guid id, ApproveSalesOrderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await salesOrderService.ApproveAsync(id, request, cancellationToken));
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
    public async Task<ActionResult<SalesOrderDto>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await salesOrderService.CancelAsync(id, cancellationToken));
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
