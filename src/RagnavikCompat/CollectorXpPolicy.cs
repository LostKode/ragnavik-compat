namespace RagnavikCompat;

internal static class CollectorXpPolicy
{
    internal static int ConfirmedUnits(int before, int after, bool owner) =>
        owner && before > 0 && after == 0 ? before : 0;
}
