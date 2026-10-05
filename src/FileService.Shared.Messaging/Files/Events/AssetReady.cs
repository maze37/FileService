namespace IntegrationEvents.Files.Events;

public record AssetReady(
    Guid AssetId,
    Guid EntityId,
    string EntityType,
    string AssetType,
    DateTimeOffset OccurredAt);