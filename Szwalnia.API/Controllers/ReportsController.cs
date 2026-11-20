using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Szwalnia.API.DTO;
using Szwalnia.API.Services;

namespace Szwalnia.API.Controllers;

[ApiController, Authorize(Roles = UserRole.Admin)]
[Route("api/reports")]
public class ReportsController(ReportService reports) : ControllerBase
{
    [HttpGet]
    public Task<List<ReportSummary>> List(CancellationToken ct) => reports.List(ct);

    [HttpGet("{id:int}")]
    public Task<ReportResponse> Get(int id, CancellationToken ct) => reports.Get(id, ct);

    [HttpPost]
    public async Task<IActionResult> Create(ReportRequest request, CancellationToken ct)
    {
        var job = await reports.Queue(request.WeekStart, UserId, ct);
        return AcceptedAtAction(nameof(Get), new { id = job.Id }, job);
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
