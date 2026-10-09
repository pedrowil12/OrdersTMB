namespace OrdersTMB.Models;

public class LogAudit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; } // Usuário relacionado à entidade auditada
    public Guid? EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Table { get; set; } = string.Empty;
    public string Dado { get; set; } = string.Empty; // Alteração registrada em JSON
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
}
