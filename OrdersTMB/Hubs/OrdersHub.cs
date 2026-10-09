using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace OrdersTMB.Hubs;

[Authorize]
public sealed class OrdersHub : Hub
{
    public const string AdminGroup = "orders:admins";

    public static string CustomerGroup(Guid customerId) => $"orders:customer:{customerId}";

    //Quando um cliente se conecta, ele é adicionado a um grupo de clientes, e quando um admin se conecta, ele é adicionado a um grupo de admins
    public override async Task OnConnectedAsync()
    {
        if (Context.User?.IsInRole("admin") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup);
        }
        else
        {
            var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(value, out var customerId))
            {
                Context.Abort();
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, CustomerGroup(customerId));
        }

        await base.OnConnectedAsync();
    }
}
