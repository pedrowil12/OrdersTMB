namespace OrdersTMB.Contracts
{
    // Representa cada mensagem individual da conversa (quem falou e o conteúdo)
    public record ChatMessageDTO(string Role, string Content);

    // Representa a requisição completa que o React enviará para a API contendo todo o histórico
    public record ChatRequest(List<ChatMessageDTO> History);
}
