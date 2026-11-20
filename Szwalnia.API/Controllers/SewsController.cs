using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Szwalnia.API.DTO;
using Szwalnia.API.Services;

namespace Szwalnia.API.Controllers;

[ApiController, Authorize(Roles = UserRole.SewingAccess)]
[Route("api/sews")]
public class SewsController(ProductionService production, ProductionQueries queries) : ControllerBase
{
    [HttpGet]
    public Task<Page<SewResponse>> List([Range(1, 1000000)] int page = 1, [StringLength(150)] string? search = null,
        [StringLength(30)] string? status = null, [Range(1, int.MaxValue)] int? seamstressId = null, CancellationToken ct = default) =>
        queries.Sews(page, search, status, seamstressId, ct);

    [HttpGet("{id:int}")]
    public Task<SewResponse> Get(int id, CancellationToken ct) => queries.Sew(id, ct);

    [HttpPost]
    public async Task<IActionResult> Create(SewRequest request)
    {
        var order = await production.CreateSew(request, UserId);
        return CreatedAtAction(nameof(Get), new { id = order.Id }, new { order.Id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Edit(int id, SewEditRequest request)
    {
        await production.EditSew(id, request);
        return NoContent();
    }

    [HttpPost("{id:int}/status")]
    public async Task<IActionResult> Status(int id, StatusRequest request)
    {
        await production.ChangeSew(id, request, UserId);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery, Required] string version)
    {
        await production.DeleteSew(id, version);
        return NoContent();
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
