namespace RagnavikCompat;

public static class AzuEpiQuickSlotRecoveryCompatibility
{
    public static readonly System.Version SupportedVersion = new(2, 6, 0);

    public static bool Supports(System.Version? version) => (version == SupportedVersion || version == new System.Version(2, 5, 1));
}
