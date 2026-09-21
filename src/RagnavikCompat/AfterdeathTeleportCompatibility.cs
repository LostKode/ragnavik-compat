namespace RagnavikCompat;

public static class AfterdeathTeleportCompatibility
{
    public static readonly System.Version SupportedAfterdeathVersion = new(1, 0, 10);

    public static bool Supports(System.Version? afterdeathVersion) =>
        afterdeathVersion == SupportedAfterdeathVersion;

    public static bool ShouldAllowTeleport(bool hasAfterdeathGhostMarker, bool isDead) =>
        hasAfterdeathGhostMarker && !isDead;
}
