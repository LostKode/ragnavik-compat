namespace RagnavikCompat;

public static class AzuEpiQuickSlotRecoveryCompatibility
{
    public static readonly System.Version SupportedVersion = new(2, 5, 1);

    public static bool Supports(System.Version? version) => version == SupportedVersion;
}
