namespace CompanyIntel.Api.Models;

public sealed class IngestionRecord
{
    public int Id { get; set; }
    public string FileName { get; set; } = "";
    public DateTime IngestedAt { get; set; }
    public int ChunkCount { get; set; }
    public int PageCount { get; set; }
    public long FileSizeBytes { get; set; }
    public string Status { get; set; } = "completed";
    public string? ErrorMessage { get; set; }
}
