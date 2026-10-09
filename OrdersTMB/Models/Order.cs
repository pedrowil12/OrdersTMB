namespace OrdersTMB.Models;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public decimal ValorTotal { get; set; }
    public string Status { get; set; } = "Pendente"; // Pendente, Processando, Finalizado
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    // Propriedade de Navegação (Para o EF Core saber que um pedido pertence a um usuário)
    public User? User { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}
