namespace OrdersTMB.Services;

public static class PasswordService
{
    // Transforma a senha "123456" em algo ambaralhado
    public static string Criptografar(string senha)
    {
        return BCrypt.Net.BCrypt.HashPassword(senha);
    }

    // Verifica se a senha digitada bate com o que está salvo no banco
    public static bool Verificar(string senhaDigitada, string hashDoBanco)
    {
        return BCrypt.Net.BCrypt.Verify(senhaDigitada, hashDoBanco);
    }
}
