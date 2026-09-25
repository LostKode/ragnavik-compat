using System.Collections.Generic;
using System.Linq;

namespace RagnavikCompat;

// One outstanding request per networked resource; only its owner can acknowledge it.
internal sealed class HarvestXpLedger
{
    private readonly Dictionary<string, (long Owner, float At, int Xp)> pending = new();
    internal void Begin(string id, long owner, float now, int xp)
    {
        foreach (var key in pending.Where(x => now - x.Value.At > 30f).Select(x => x.Key).ToArray())
            pending.Remove(key);
        if (!pending.ContainsKey(id)) pending[id] = (owner, now, xp);
    }
    internal int Confirm(string id, long sender, float now)
    {
        if (!pending.TryGetValue(id, out var value) || value.Owner != sender) return 0;
        pending.Remove(id);
        return now - value.At <= 30f && now >= value.At ? value.Xp : 0;
    }
    internal void Cancel(string id) => pending.Remove(id);
    internal void Clear() => pending.Clear();
}
