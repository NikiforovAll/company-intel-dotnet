namespace CompanyIntel.Api.Models;

public sealed class ChatSuggestion
{
    public int Id { get; set; }
    public string Text { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
