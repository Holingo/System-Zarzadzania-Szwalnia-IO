using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Szwalnia.API.DTO;
using Szwalnia.API.Services;

namespace Szwalnia.API.Controllers;

[ApiController, Authorize]
[Route("api")]
public class OverviewController(ProductionQueries queries) : ControllerBase
{
    [HttpGet("dashboard")]
    public Task<DashboardResponse> Dashboard(CancellationToken ct) => queries.Dashboard(ct);

    [HttpGet("history/{resource}/{id:int}")]
    public Task<List<HistoryResponse>> History(string resource, int id, CancellationToken ct) =>
        queries.History(resource, id, ct);
}
