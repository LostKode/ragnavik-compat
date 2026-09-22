namespace RagnavikCompat;

public static class EpicLootMagicPluginTakeAllCompatibility
{
    public static bool Supports(System.Version? epicLootVersion, System.Version? magicPluginVersion) =>
        epicLootVersion != null && magicPluginVersion != null;
}
