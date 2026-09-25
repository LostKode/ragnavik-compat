namespace RagnavikCompat;

public static class AfterdeathNearestBedCompatibility
{
    public static readonly System.Version SupportedAfterdeathVersion = new(1, 0, 11);

    public static bool Supports(System.Version? afterdeathVersion) =>
        afterdeathVersion == SupportedAfterdeathVersion;

    public static bool ShouldUseBed(
        bool hasValidBed,
        float deathX,
        float deathZ,
        float skathiX,
        float skathiZ,
        float bedX,
        float bedZ)
    {
        if (!hasValidBed)
            return false;

        var skathiDistanceSquared = DistanceSquared(deathX, deathZ, skathiX, skathiZ);
        var bedDistanceSquared = DistanceSquared(deathX, deathZ, bedX, bedZ);
        return bedDistanceSquared < skathiDistanceSquared;
    }

    private static double DistanceSquared(float fromX, float fromZ, float toX, float toZ)
    {
        var deltaX = (double)toX - fromX;
        var deltaZ = (double)toZ - fromZ;
        return deltaX * deltaX + deltaZ * deltaZ;
    }
}
