namespace FileService.Contracts.Dtos;

public record CheckMediaAssetExistsAndReadyResponse(
    bool AssetExists,
    bool IsReady,
    string ContentType);