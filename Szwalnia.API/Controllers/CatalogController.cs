using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Szwalnia.API.DTO;
using Szwalnia.API.Services;

namespace Szwalnia.API.Controllers;

[ApiController, Authorize(Roles = UserRole.All)]
[Route("api/catalog")]
public class CatalogController(CatalogService catalog) : ControllerBase
{
    [HttpGet("options")]
    public Task<CatalogOptions> Options(CancellationToken ct) => catalog.Options(
        User.IsInRole(UserRole.Admin) || User.IsInRole(UserRole.Cutting),
        User.IsInRole(UserRole.Admin) || User.IsInRole(UserRole.Sewing), ct);

    [HttpPost("materials"), Authorize(Roles = UserRole.Admin)]
    public async Task<IActionResult> CreateMaterial(MaterialRequest request)
    {
        var id = await catalog.SaveMaterial(null, null, request);
        return StatusCode(201, new { id });
    }

    [HttpPut("materials/{id:int}"), Authorize(Roles = UserRole.Admin)]
    public async Task<IActionResult> EditMaterial(int id, [FromQuery, Required] string version, MaterialRequest request)
    {
        await catalog.SaveMaterial(id, version, request);
        return NoContent();
    }

    [HttpDelete("materials/{id:int}"), Authorize(Roles = UserRole.Admin)]
    public async Task<IActionResult> DeleteMaterial(int id, [FromQuery, Required] string version)
    {
        await catalog.Delete<Material>(id, version);
        return NoContent();
    }

    [HttpPost("products"), Authorize(Roles = UserRole.Admin)]
    public async Task<IActionResult> CreateProduct(ProductRequest request)
    {
        var id = await catalog.SaveProduct(null, null, request);
        return StatusCode(201, new { id });
    }

    [HttpPut("products/{id:int}"), Authorize(Roles = UserRole.Admin)]
    public async Task<IActionResult> EditProduct(int id, [FromQuery, Required] string version, ProductRequest request)
    {
        await catalog.SaveProduct(id, version, request);
        return NoContent();
    }

    [HttpDelete("products/{id:int}"), Authorize(Roles = UserRole.Admin)]
    public async Task<IActionResult> DeleteProduct(int id, [FromQuery, Required] string version)
    {
        await catalog.Delete<Product>(id, version);
        return NoContent();
    }

    [HttpPost("rolls"), Authorize(Roles = UserRole.Admin)]
    public async Task<IActionResult> CreateRoll(RollRequest request)
    {
        var id = await catalog.SaveRoll(null, null, request);
        return StatusCode(201, new { id });
    }

    [HttpPut("rolls/{id:int}"), Authorize(Roles = UserRole.Admin)]
    public async Task<IActionResult> EditRoll(int id, [FromQuery, Required] string version, RollRequest request)
    {
        await catalog.SaveRoll(id, version, request);
        return NoContent();
    }

    [HttpDelete("rolls/{id:int}"), Authorize(Roles = UserRole.Admin)]
    public async Task<IActionResult> DeleteRoll(int id, [FromQuery, Required] string version)
    {
        await catalog.Delete<Roll>(id, version);
        return NoContent();
    }

    [HttpPost("seamstresses"), Authorize(Roles = UserRole.Admin)]
    public async Task<IActionResult> CreateSeamstress(SeamstressRequest request)
    {
        var id = await catalog.SaveSeamstress(null, null, request);
        return StatusCode(201, new { id });
    }

    [HttpPut("seamstresses/{id:int}"), Authorize(Roles = UserRole.Admin)]
    public async Task<IActionResult> EditSeamstress(int id, [FromQuery, Required] string version, SeamstressRequest request)
    {
        await catalog.SaveSeamstress(id, version, request);
        return NoContent();
    }

    [HttpDelete("seamstresses/{id:int}"), Authorize(Roles = UserRole.Admin)]
    public async Task<IActionResult> DeleteSeamstress(int id, [FromQuery, Required] string version)
    {
        await catalog.Delete<Seamstress>(id, version);
        return NoContent();
    }
}
