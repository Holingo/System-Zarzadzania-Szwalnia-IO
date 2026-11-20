using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Szwalnia.API.DTO;
using Szwalnia.API.Services;

namespace Szwalnia.API.Controllers;

[ApiController, Authorize]
[Route("api")]
public class OverviewController(ProductionQueries queries) : ControllerBase
{
    [HttpGet("dashboard"), Authorize(Roles = UserRole.All)]
    public Task<DashboardResponse> Dashboard(CancellationToken ct) => queries.Dashboard(
        User.IsInRole(UserRole.Admin) || User.IsInRole(UserRole.Cutting),
        User.IsInRole(UserRole.Admin) || User.IsInRole(UserRole.Sewing), ct);

    [HttpGet("history/cuts/{id:int}"), Authorize(Roles = UserRole.CuttingAccess)]
    public Task<List<HistoryResponse>> CutHistory(int id, CancellationToken ct) => queries.History("cuts", id, ct);

    [HttpGet("history/sews/{id:int}"), Authorize(Roles = UserRole.SewingAccess)]
    public Task<List<HistoryResponse>> SewHistory(int id, CancellationToken ct) => queries.History("sews", id, ct);
}
