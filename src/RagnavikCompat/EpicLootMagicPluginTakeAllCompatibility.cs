namespace RagnavikCompat;

public static class EpicLootMagicPluginTakeAllCompatibility
{
    public static readonly System.Version SupportedEpicLootVersion = new(0, 14, 10);
    public static readonly System.Version SupportedMagicPluginVersion = new(2, 2, 0);

    public static bool Supports(System.Version? epicLootVersion, System.Version? magicPluginVersion) =>
        epicLootVersion == SupportedEpicLootVersion &&
        magicPluginVersion == SupportedMagicPluginVersion;
}
