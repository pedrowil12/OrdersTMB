namespace OrdersTMB.Models;

public class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantidade { get; set; }
    public decimal Price { get; set; } // Salva o preço do produto no momento da compra

    // Propriedades de Navegação
    public Product? Product { get; set; }
}
