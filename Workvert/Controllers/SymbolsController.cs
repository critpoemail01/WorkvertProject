using Workvert.Models;
using Workvert.Services;
using Microsoft.AspNetCore.Mvc;

namespace Workvert.Controllers;

[Route("symbols")]
public sealed class SymbolsController : Controller
{
    private readonly ISymbolCatalogService _symbols;

    public SymbolsController(ISymbolCatalogService symbols)
    {
        _symbols = symbols;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] MarketType market, [FromQuery] string q, CancellationToken ct)
    {
        var results = await _symbols.SearchAsync(market, q ?? string.Empty, ct);
        return Ok(results);
    }

    [HttpGet("validate")]
    public async Task<IActionResult> Validate([FromQuery] MarketType market, [FromQuery] string symbol, CancellationToken ct)
    {
        var valid = await _symbols.IsValidSymbolAsync(market, symbol ?? string.Empty, ct);
        return Ok(new { valid });
    }
}
