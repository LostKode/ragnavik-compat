using System;

namespace RagnavikCompat;

internal static class EpicMmoReloadGuardCompatibility
{
    internal static readonly System.Version SupportedVersion = new(1, 9, 68);

    internal static bool Supports(System.Version? installedVersion) =>
        installedVersion != null && installedVersion == SupportedVersion;
}
