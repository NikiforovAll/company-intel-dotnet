using Microsoft.Extensions.VectorData;

namespace CompanyIntel.Api.Models;

public sealed class DocumentRecord
{
    [VectorStoreKey]
    public Guid Id { get; set; } = Guid.NewGuid();

    [VectorStoreData]
    public string Text { get; set; } = "";

    [VectorStoreData]
    public string FileName { get; set; } = "";

    [VectorStoreData]
    public int ChunkIndex { get; set; }

    [VectorStoreData(IsIndexed = true)]
    public string Source { get; set; } = "";

    [VectorStoreVector(Dimensions: 384)]
    public string Embedding => Text;
}
