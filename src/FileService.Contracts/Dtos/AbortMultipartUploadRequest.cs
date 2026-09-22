namespace FileService.Contracts.Dtos;

public record AbortMultipartUploadRequest(Guid MediaAssetId, string UploadId);