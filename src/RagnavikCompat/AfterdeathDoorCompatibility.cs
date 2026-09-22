namespace RagnavikCompat;

public static class AfterdeathDoorCompatibility
{
    public static readonly System.Version SupportedAfterdeathVersion = new(1, 0, 10);

    public static bool Supports(System.Version? afterdeathVersion) =>
        afterdeathVersion == SupportedAfterdeathVersion;

    public static bool ShouldAllowInteraction(bool hasAfterdeathGhostMarker, bool isDead, bool isDoor, bool isAssignedBed) =>
        hasAfterdeathGhostMarker && !isDead && (isDoor || isAssignedBed);
}
