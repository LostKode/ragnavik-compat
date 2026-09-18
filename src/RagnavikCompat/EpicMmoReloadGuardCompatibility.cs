using System;

namespace RagnavikCompat;

internal static class EpicMmoReloadGuardCompatibility
{
    internal static readonly Version SupportedVersion = new(1, 9, 67);

    internal static bool Supports(Version? installedVersion) =>
        installedVersion != null && installedVersion == SupportedVersion;
}
