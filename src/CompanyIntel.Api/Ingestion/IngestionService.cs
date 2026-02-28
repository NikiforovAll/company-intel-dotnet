using CompanyIntel.Api.Data;
using CompanyIntel.Api.Models;
using Microsoft.Extensions.VectorData;

namespace CompanyIntel.Api.Ingestion;

public sealed class IngestionService(
    VectorStoreCollection<Guid, DocumentRecord> collection,
    IngestionDbContext db,
    ILogger<IngestionService> logger
)
{
    public async Task<IngestionRecord> IngestPdfAsync(
        Stream pdfStream,
        string fileName,
        string source,
        long fileSizeBytes,
        CancellationToken ct = default
    )
    {
        var record = new IngestionRecord
        {
            FileName = fileName,
            IngestedAt = DateTime.UtcNow,
            FileSizeBytes = fileSizeBytes,
        };

        try
        {
            await collection.EnsureCollectionExistsAsync(ct);

            logger.LogInformation("Extracting text from {FileName}", fileName);
            var extraction = PdfTextExtractor.Extract(pdfStream);
            record.PageCount = extraction.PageCount;

            logger.LogInformation(
                "Chunking text from {FileName} ({Length} chars)",
                fileName,
                extraction.Text.Length
            );
            var chunks = TextChunker.Chunk(extraction.Text);
            record.ChunkCount = chunks.Count;

            logger.LogInformation(
                "Upserting {Count} chunks from {FileName}",
                chunks.Count,
                fileName
            );

            var records = chunks
                .Select(
                    (chunk, index) =>
                        new DocumentRecord
                        {
                            Text = chunk,
                            FileName = fileName,
                            ChunkIndex = index,
                            Source = source,
                        }
                )
                .ToList();

            await collection.UpsertAsync(records, ct);

            record.Status = "completed";
            logger.LogInformation(
                "Ingested {Count} chunks from {FileName}",
                records.Count,
                fileName
            );
        }
        catch (Exception ex)
        {
            record.Status = "failed";
            record.ErrorMessage = ex.Message;
            logger.LogError(ex, "Failed to ingest {FileName}", fileName);
        }

        db.IngestionRecords.Add(record);
        await db.SaveChangesAsync(ct);

        return record;
    }
}
