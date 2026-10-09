using OrdersTMB.Services;

namespace OrdersTMB.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Armazenados criptografados no Banco de Dados
    public string Name { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public bool Active { get; set; } = true;
    public string Role { get; set; } = "customer"; // admin ou customer
    public string SenhaHash { get; set; } = string.Empty; // Protegido com BCrypt
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    // Métodos Auxiliares para facilitar a criação do usuário no CRUD
    public void DefinirDadosProtegidos(string nomeLimpo, string telefoneLimpo, string emailLimpo, string senhaLimpa)
    {
        Name = EncryptionService.Criptografar(nomeLimpo);
        Telefone = EncryptionService.Criptografar(telefoneLimpo);
        Email = EncryptionService.Criptografar(emailLimpo);
        SenhaHash = PasswordService.Criptografar(senhaLimpa);
    }

    // Propriedades Estendidas (Não vão pro banco) para ler o dado limpo na API
    public string ObterNomeLegivel() => EncryptionService.Descriptografar(Name);
    public string ObterTelefoneLegivel() => EncryptionService.Descriptografar(Telefone);
    public string ObterEmailLegivel() => EncryptionService.Descriptografar(Email);
}
