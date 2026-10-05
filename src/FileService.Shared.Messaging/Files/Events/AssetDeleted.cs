namespace IntegrationEvents.Files.Events;

public record AssetDeleted(
    Guid AssetId,
    Guid EntityId,
    string EntityType,
    string AssetType,
    DateTimeOffset OccurredAt);