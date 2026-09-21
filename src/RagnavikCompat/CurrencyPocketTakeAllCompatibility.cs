using System;

namespace RagnavikCompat;

internal static class CurrencyPocketTakeAllCompatibility
{
    internal static readonly System.Version SupportedVersion = new(1, 0, 15);
    internal const string CoinSharedName = "$item_coins";

    internal static bool Supports(System.Version? installedVersion) =>
        installedVersion != null && installedVersion == SupportedVersion;

    internal static bool IsCoin(string? sharedName, int stack) =>
        stack > 0 && string.Equals(sharedName, CoinSharedName, StringComparison.Ordinal);

    internal static bool TryAddBalance(int currentBalance, int stack, out int updatedBalance)
    {
        updatedBalance = currentBalance;
        if (stack <= 0 || currentBalance < 0 || currentBalance > int.MaxValue - stack)
            return false;

        updatedBalance = currentBalance + stack;
        return true;
    }
}
