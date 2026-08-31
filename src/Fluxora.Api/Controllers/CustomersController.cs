using Fluxora.Application.Common;
using Fluxora.Application.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fluxora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/customers")]
public class CustomersController(CustomerService customerService, CustomerCsvService customerCsvService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> List(
        [FromQuery] string? search, [FromQuery] bool? isActive,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        return Ok(await customerService.ListAsync(search, isActive, page, pageSize, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await customerService.GetByIdAsync(id, cancellationToken));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await customerService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
        }
        catch (ConflictException ex)
        {
            return Conflict(new ProblemDetails { Title = ex.Message, Status = StatusCodes.Status409Conflict });
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> Update(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await customerService.UpdateAsync(id, request, cancellationToken));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await customerService.SetActiveAsync(id, active: false, cancellationToken);
            return NoContent();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await customerService.SetActiveAsync(id, active: true, cancellationToken);
            return NoContent();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("import")]
    [Authorize(Policy = AppPolicies.DataExchangeManage)]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<CustomerCsvImportResult>> Import(
        IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return ValidationProblem("A non-empty CSV file is required.");
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
        {
            return ValidationProblem("The uploaded file must have a .csv extension.");
        }

        try
        {
            await using var stream = file.OpenReadStream();
            return Ok(await customerCsvService.ImportAsync(stream, cancellationToken));
        }
        catch (InvalidCsvException exception)
        {
            return ValidationProblem(exception.Message);
        }
    }

    [HttpGet("export")]
    [Authorize(Policy = AppPolicies.DataExchangeManage)]
    public async Task<IActionResult> Export(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var export = await customerCsvService.ExportAsync(search, isActive, cancellationToken);
        return File(export.Content, export.ContentType, export.FileName);
    }
}
