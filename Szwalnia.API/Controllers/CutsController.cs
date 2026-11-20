using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Szwalnia.API.DTO;
using Szwalnia.API.Services;

namespace Szwalnia.API.Controllers;

[ApiController, Authorize]
[Route("api/cuts")]
public class CutsController(ProductionService production, ProductionQueries queries) : ControllerBase
{
    [HttpGet]
    public Task<Page<CutResponse>> List([Range(1, 1000000)] int page = 1, [StringLength(150)] string? search = null,
        [StringLength(30)] string? status = null, CancellationToken ct = default) =>
        queries.Cuts(page, search, status, ct);

    [HttpGet("{id:int}")]
    public Task<CutResponse> Get(int id, CancellationToken ct) => queries.Cut(id, ct);

    [HttpPost]
    public async Task<IActionResult> Create(CutRequest request)
    {
        var order = await production.CreateCut(request, UserId);
        return CreatedAtAction(nameof(Get), new { id = order.Id }, new { order.Id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Edit(int id, CutEditRequest request)
    {
        await production.EditCut(id, request);
        return NoContent();
    }

    [HttpPost("{id:int}/status")]
    public async Task<IActionResult> Status(int id, CutStatusRequest request)
    {
        await production.ChangeCut(id, request, UserId);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery, Required] string version)
    {
        await production.DeleteCut(id, version);
        return NoContent();
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
