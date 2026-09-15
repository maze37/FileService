namespace FileService.Contracts;

public record AbortMultipartUploadRequest(Guid MediaAssetId, string UploadId);