namespace RagnavikCompat;

public static class AfterdeathStarvationCompatibility
{
    public static readonly System.Version SupportedAfterdeathVersion = new(1, 0, 11);
    public static readonly System.Version SupportedStarvationVersion = new(1, 0, 5);

    public static bool Supports(System.Version? afterdeathVersion, System.Version? starvationVersion) =>
        afterdeathVersion == SupportedAfterdeathVersion &&
        starvationVersion == SupportedStarvationVersion;

    public static bool ShouldRunStarvation(bool hasAfterdeathGhostMarker, bool isDead) =>
        !hasAfterdeathGhostMarker || isDead;
}
