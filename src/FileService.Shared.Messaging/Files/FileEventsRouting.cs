namespace IntegrationEvents.Files;

public static class FileEventsRouting
{
    public const string EXCHANGE = "file-events";

    public static class RoutingKeys
    {
        public const string ALL_ASSET_EVENTS = "asset.*.*";
        
        public static string AssetReady(string entityType) => $"asset.ready.{entityType.ToLowerInvariant()}";

        public static string AssetDeleted(string entityType) => $"asset.deleted.{entityType.ToLowerInvariant()}";
    }
}