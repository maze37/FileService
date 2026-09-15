namespace FileService.Contracts;

public record CompleteMultipartUploadRequest(
    Guid MediaAssetId,
    string UploadId,
    IReadOnlyList<PartETagDto> PartETags);