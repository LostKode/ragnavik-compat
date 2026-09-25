using RagnavikCompat;

var ledger = new HarvestXpLedger();
static void Eq(int expected, int actual, string scenario)
{
    if (expected != actual) throw new Exception($"{scenario}: expected {expected}, got {actual}");
    Console.WriteLine("PASS: " + scenario);
}
for (var i = 0; i < 100; i++) ledger.Begin("crop" + i, 42, 1, 1);
var total = Enumerable.Range(0, 100).Sum(i => ledger.Confirm("crop" + i, 42, 1.01f));
Eq(100, total, "100 crops harvested together each award XP without a global cooldown");
Eq(0, ledger.Confirm("crop0", 42, 1.02f), "duplicate acknowledgment gives no XP");
ledger.Begin("contested", 42, 2, 1);
ledger.Begin("contested", 42, 3, 999);
Eq(0, ledger.Confirm("contested", 43, 3), "non-owner cannot acknowledge harvest");
Eq(1, ledger.Confirm("contested", 42, 3), "repeated clicks do not replace or multiply original reward");
Eq(0, ledger.Confirm("bystander", 42, 3), "client without a harvest request gets no XP");
ledger.Begin("expired", 42, 1, 1);
Eq(0, ledger.Confirm("expired", 42, 32), "stale harvest request gives no XP");
ledger.Begin("failed", 42, 3, 1); ledger.Cancel("failed");
Eq(0, ledger.Confirm("failed", 42, 4), "cancelled or failed interaction gives no XP");
ledger.Begin("session", 42, 3, 1); ledger.Clear();
Eq(0, ledger.Confirm("session", 42, 4), "session exit clears outstanding harvests");
ledger.Begin("respawn", 42, 4, 1);
Eq(1, ledger.Confirm("respawn", 42, 5), "first harvest succeeds");
ledger.Begin("respawn", 42, 500, 1);
Eq(1, ledger.Confirm("respawn", 42, 501), "regrown resource can award XP again");
ledger.Begin("clock", 42, 10, 1);
Eq(0, ledger.Confirm("clock", 42, 9), "backwards session clock gives no XP");
