using System.Security.Cryptography;
using System.Text;

namespace OrdersTMB.Services;

public static class EncryptionService
{
    private static string _chaveSecreta = string.Empty;

    public static void Inicializar(string? chave)
    {
        if (string.IsNullOrWhiteSpace(chave))
        {
            throw new ArgumentNullException(nameof(chave), "A chave secreta de criptografia não foi configurada nas variáveis de ambiente!");
        }

        // Garante que a chave tenha exatamente 32 caracteres (requisito do AES-256)
        _chaveSecreta = chave.PadRight(32).Substring(0, 32);
    }

    public static string Criptografar(string textoLimpo)
    {
        if (string.IsNullOrWhiteSpace(textoLimpo)) return string.Empty;
        if (string.IsNullOrEmpty(_chaveSecreta)) throw new InvalidOperationException("EncryptionService não foi inicializado.");

        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(_chaveSecreta);
        aes.IV = new byte[16]; // Vetor de inicialização zerado para buscas exatas no banco

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            using var sw = new StreamWriter(cs);
            sw.Write(textoLimpo);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public static string Descriptografar(string textoCriptografado)
    {
        if (string.IsNullOrWhiteSpace(textoCriptografado)) return string.Empty;
        if (string.IsNullOrEmpty(_chaveSecreta)) throw new InvalidOperationException("EncryptionService não foi inicializado.");

        try
        {
            using Aes aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_chaveSecreta);
            aes.IV = new byte[16];

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(Convert.FromBase64String(textoCriptografado));
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }
        catch
        {
            return "[Erro ao descriptografar dado]";
        }
    }
}
