using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersTMB.Contracts;
using OrdersTMB.Data;
using OrdersTMB.Models;
using OrdersTMB.Services;

namespace OrdersTMB.Controllers;

[ApiController]
[Authorize(Roles = "admin")]
[Route("customers")]
public sealed class CustomersController(AppDbContext context) : ControllerBase
{


    //Lista de clientes
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<CustomerResponse>>> List(
        CancellationToken cancellationToken)
    {
        var customers = await context.Users
            .AsNoTracking()
            .Where(user => user.Role == "customer")
            .OrderByDescending(user => user.DataCriacao)
            .ToListAsync(cancellationToken);

        return Ok(customers.Select(Map).ToList());
    }

    //Busca cliente por id
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var customer = await context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user => user.Id == id && user.Role == "customer",
                cancellationToken);

        return customer is null ? NotFound() : Ok(Map(customer));
    }

    //Cria cliente
    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> Create(
        CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var encryptedEmail = EncryptionService.Criptografar(normalizedEmail);

        if (await context.Users.AnyAsync(user => user.Email == encryptedEmail, cancellationToken))
        {
            return Conflict(new { message = "Já existe um usuário com este e-mail." });
        }

        var customer = new User { Role = "customer" };
        customer.DefinirDadosProtegidos(
            request.Name.Trim(),
            request.Phone.Trim(),
            normalizedEmail,
            request.Password);

        context.Users.Add(customer);
        await context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, Map(customer));
    }

    // Atualiza cliente
    private static CustomerResponse Map(User user) => new(
        user.Id,
        user.ObterNomeLegivel(),
        user.ObterTelefoneLegivel(),
        user.ObterEmailLegivel(),
        user.Active,
        user.DataCriacao);
}
