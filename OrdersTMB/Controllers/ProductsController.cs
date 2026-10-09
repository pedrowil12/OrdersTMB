using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersTMB.Contracts;
using OrdersTMB.Data;
using OrdersTMB.Models;

namespace OrdersTMB.Controllers;

[ApiController]
[Authorize]
[Route("products")]
public sealed class ProductsController(AppDbContext context) : ControllerBase
{
    // Lista de produtos ativos
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ProductResponse>>> List(
        CancellationToken cancellationToken)
    {
        var products = await context.Products
            .AsNoTracking()
            .Where(product => product.Active)
            .OrderBy(product => product.Name)
            .ToListAsync(cancellationToken);

        return Ok(products.Select(Map).ToList());
    }

    // Busca produto por ID
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var product = await context.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return product is null ? NotFound() : Ok(Map(product));
    }

    // Cria um novo produto
    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await context.Products.AnyAsync(
                product => product.Name.ToLower() == name.ToLower(),
                cancellationToken))
        {
            return Conflict(new { message = "Já existe um produto com este nome." });
        }

        var product = new Product
        {
            Name = name,
            Price = request.Price
        };

        context.Products.Add(product);
        await context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, Map(product));
    }

    private static ProductResponse Map(Product product) => new(
        product.Id,
        product.Name,
        product.Price,
        product.Active);
}
