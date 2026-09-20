namespace FileService.Contracts;

public record StartMultipartUploadResponse(
    Guid MediaAssetId,
    string UploadId, 
    IReadOnlyList<ChunkUploadUrl> ChunkUploadUrls,
    int ChunkSize);