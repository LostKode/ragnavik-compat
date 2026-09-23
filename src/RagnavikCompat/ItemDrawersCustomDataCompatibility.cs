namespace RagnavikCompat;

public static class ItemDrawersCustomDataCompatibility
{
    public const string PluginGuid = "kg.ItemDrawers";

    public static bool Supports(System.Version? installedVersion, bool hasRequiredApi) =>
        installedVersion != null && hasRequiredApi;
}
