using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Rpg.Gameplay;

// Actual production rules/codec/store; no Unity or serialization substitutes.
internal static class ProgressionChecks
{
    private const string RewardA = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string RewardB = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private const string RewardC = "cccccccccccccccccccccccccccccccc";
    private static int passed;
    private static int failed;

    private static void Check(string name, Action test)
    {
        try { test(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + ": " + error.Message); }
    }

    private static void Require(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }

    private static int Spent(int rank)
    {
        return rank == 0 ? 0 : rank == 1 ? 30 : rank == 2 ? 75 : 135;
    }

    private static ClearingProgressData Data(int clears, int vitality, int focus, string reward)
    {
        return new ClearingProgressData(clears + vitality + focus,
            clears * 30 - Spent(vitality) - Spent(focus), clears, vitality, focus, reward);
    }

    private static ClearingProgressData Fresh()
    {
        return Data(0, 0, 0, "");
    }

    private static string StagingPath(FileClearingProgressStore store)
    {
        return Path.ChangeExtension(store.PrimaryPath, ".tmp");
    }

    private static void ThrowsArgument(Action operation, string message)
    {
        bool threw = false;
        try { operation(); } catch (ArgumentException) { threw = true; }
        Require(threw, message);
    }

    private static void EqualData(ClearingProgressData expected, ClearingProgressData actual, string message)
    {
        Require(actual != null && expected.Revision == actual.Revision
            && expected.BankCoins == actual.BankCoins && expected.ClearedRuns == actual.ClearedRuns
            && expected.VitalityRank == actual.VitalityRank && expected.FocusRank == actual.FocusRank
            && expected.LastRewardId == actual.LastRewardId, message);
    }

    private static ClearingRun CompletedRun()
    {
        var run = new ClearingRun();
        for (int strike = 0; strike < 2; strike++)
        {
            run.Advance(ClearingRun.BurstCooldown);
            long token;
            Require(run.BeginPlayerAttack(PlayerAttackKind.Burst, 20f, out token), "clear setup attack rejected");
            for (int enemy = 0; enemy < ClearingRun.SentinelCount; enemy++)
                Require(run.TryHitSentinel(token, enemy), "clear setup contact rejected");
        }
        Require(run.GateUnlocked && run.RewardCoins == 30 && run.TryCompleteObjective(), "clear setup incomplete");
        return run;
    }

    private static string Reseal(string text)
    {
        int start = text.IndexOf("checksum=", StringComparison.Ordinal);
        Require(start >= 0, "fixture expected a checksum field");
        string body = text.Substring(0, start);
        byte[] digest;
        using (var sha = SHA256.Create()) digest = sha.ComputeHash(Encoding.UTF8.GetBytes(body));
        var hex = new StringBuilder(64);
        foreach (byte value in digest) hex.Append(value.ToString("x2", CultureInfo.InvariantCulture));
        return body + "checksum=" + hex + "\n";
    }

    private static void Rejected(string text, string message)
    {
        ProgressLoadResult result = ClearingProgressCodec.Decode(text);
        Require(!result.CanWrite && result.Kind == ProgressLoadKind.Unavailable, message);
    }

    // Test port for deterministic domain transaction failures; real IO is tested below.
    private sealed class RecordingStore : IClearingProgressStore
    {
        public ClearingProgressData Stored = Fresh();
        public ProgressLoadKind Kind = ProgressLoadKind.New;
        public bool Writable = true;
        public bool FailWrites;
        public int SaveCalls;

        public ProgressLoadResult Load() { return new ProgressLoadResult(Stored, Kind, Writable); }
        public bool TrySave(ClearingProgressData expected, ClearingProgressData next)
        {
            SaveCalls++;
            if (!Writable || FailWrites || expected.Revision != Stored.Revision
                || expected.LastRewardId != Stored.LastRewardId) return false;
            Stored = next;
            Kind = ProgressLoadKind.Loaded;
            return true;
        }
    }

    private static void InDirectory(Action<string> test)
    {
        string directory = Path.Combine(Path.GetTempPath(), "rpg-progression-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try { test(directory); }
        finally { Directory.Delete(directory, true); }
    }

    private static void SeedTwoSaves(FileClearingProgressStore store)
    {
        Require(store.Load().Kind == ProgressLoadKind.New, "fresh file store not new");
        Require(store.TrySave(Fresh(), Data(1, 0, 0, RewardA)), "initial real save failed");
        Require(store.TrySave(Data(1, 0, 0, RewardA), Data(2, 0, 0, RewardB)), "atomic replacement failed");
    }

    private static int Main()
    {
        Check("fresh progress has no coins, ranks or bonuses", delegate {
            var store = new RecordingStore();
            var progress = new ClearingProgress(store);
            EqualData(Fresh(), progress.Data, "fresh data differs");
            Require(progress.LoadKind == ProgressLoadKind.New && progress.CanWrite && !progress.SaveFailed,
                "fresh availability incorrect");
            Require(progress.BonusHealth == 0 && progress.BonusMana == 0, "fresh bonuses nonzero");
            Require(progress.Price(ClearingUpgrade.Vitality) == 30 && progress.Price(ClearingUpgrade.Focus) == 30,
                "initial prices incorrect");
            Require(!progress.IsCompletionBanked(RewardA), "unearned reward is banked");
        });
        Check("invalid ledger construction fails before data enters a transaction", delegate {
            ThrowsArgument(delegate { new ClearingProgressData(0, 30, 0, 0, 0, ""); }, "unearned coins constructible");
            ThrowsArgument(delegate { new ClearingProgressData(1, 30, 1, 4, 0, RewardA); }, "excess rank constructible");
            ThrowsArgument(delegate { new ClearingProgressData(1, -1, 1, 0, 0, RewardA); }, "negative balance constructible");
            ThrowsArgument(delegate { new ClearingProgressData(1, 30, 1, 0, 0, null); }, "missing reward constructible");
            ThrowsArgument(delegate { new ClearingProgressData(9, 30, 1, 0, 0, RewardA); }, "inconsistent revision constructible");
            ThrowsArgument(delegate { new ClearingProgress(null); }, "missing store accepted");
        });
        Check("kill rewards do not bank before beacon completion", delegate {
            var store = new RecordingStore(); var progress = new ClearingProgress(store);
            var run = CompletedRun(); run.ResetRun();
            long token; run.BeginPlayerAttack(PlayerAttackKind.Burst, 20f, out token);
            for (int i = 0; i < 3; i++) run.TryHitSentinel(token, i);
            run.Advance(ClearingRun.BurstCooldown); run.BeginPlayerAttack(PlayerAttackKind.Burst, 20f, out token);
            for (int i = 0; i < 3; i++) run.TryHitSentinel(token, i);
            Require(run.GateUnlocked && run.RewardCoins == 30 && !run.IsComplete, "pre-beacon setup invalid");
            Require(progress.BankCompletion(run, RewardA) == ProgressActionResult.RunIncomplete,
                "kills bypassed beacon deposit");
            EqualData(Fresh(), progress.Data, "premature banking changed data");
            Require(store.SaveCalls == 0, "premature deposit attempted a write");
        });
        Check("death and null runs cannot bank rewards", delegate {
            var store = new RecordingStore(); var progress = new ClearingProgress(store);
            var run = new ClearingRun(); run.NotifyPlayerDeath();
            Require(progress.BankCompletion(run, RewardA) == ProgressActionResult.RunIncomplete
                && progress.BankCompletion(null, RewardA) == ProgressActionResult.RunIncomplete,
                "dead or absent run banked");
            Require(store.SaveCalls == 0 && progress.Data.BankCoins == 0, "rejection mutated persistence");
        });
        Check("beacon completion deposits exactly thirty coins and one clear", delegate {
            var store = new RecordingStore(); var progress = new ClearingProgress(store);
            Require(progress.BankCompletion(CompletedRun(), RewardA) == ProgressActionResult.Saved, "deposit failed");
            EqualData(Data(1, 0, 0, RewardA), progress.Data, "deposit ledger wrong");
            EqualData(progress.Data, store.Stored, "service published before persistent save");
            Require(progress.IsCompletionBanked(RewardA) && store.SaveCalls == 1, "deposit identity lost");
        });
        Check("repeated completion ID cannot duplicate a deposited reward", delegate {
            var store = new RecordingStore(); var progress = new ClearingProgress(store); var run = CompletedRun();
            progress.BankCompletion(run, RewardA);
            for (int i = 0; i < 10; i++)
                Require(progress.BankCompletion(run, RewardA) == ProgressActionResult.AlreadyBanked, "duplicate accepted");
            Require(progress.Data.BankCoins == 30 && progress.Data.ClearedRuns == 1 && store.SaveCalls == 1,
                "duplicate changed economy or wrote again");
        });
        Check("reload retains the last deposit ID across an upgrade purchase", delegate {
            var store = new RecordingStore(); var progress = new ClearingProgress(store);
            progress.BankCompletion(CompletedRun(), RewardA);
            Require(progress.Buy(ClearingUpgrade.Vitality) == ProgressActionResult.Saved, "first purchase failed");
            var reloaded = new ClearingProgress(store);
            Require(reloaded.IsCompletionBanked(RewardA)
                && reloaded.BankCompletion(CompletedRun(), RewardA) == ProgressActionResult.AlreadyBanked,
                "purchase or reload lost deposit identity");
            EqualData(Data(1, 1, 0, RewardA), reloaded.Data, "reloaded purchase differs");
        });
        Check("different completed runs accumulate persistent coins", delegate {
            var progress = new ClearingProgress(new RecordingStore());
            progress.BankCompletion(CompletedRun(), RewardA);
            Require(progress.BankCompletion(CompletedRun(), RewardB) == ProgressActionResult.Saved, "second run rejected");
            EqualData(Data(2, 0, 0, RewardB), progress.Data, "second-run ledger wrong");
        });
        Check("malformed completion identities reject without touching the ledger", delegate {
            var store = new RecordingStore(); var progress = new ClearingProgress(store); var run = CompletedRun();
            foreach (string id in new[] { null, "", "not-a-guid", new string('z', 32) })
                Require(progress.BankCompletion(run, id) == ProgressActionResult.Unavailable,
                    "malformed completion identity accepted");
            EqualData(Fresh(), progress.Data, "identity rejection changed coins");
            Require(store.SaveCalls == 0, "invalid identity reached persistence");
        });
        Check("completion identities normalize letter case without granting a duplicate reward", delegate {
            var store = new RecordingStore(); var progress = new ClearingProgress(store); var run = CompletedRun();
            Require(progress.BankCompletion(run, RewardA.ToUpperInvariant()) == ProgressActionResult.Saved,
                "valid uppercase identity rejected");
            EqualData(Data(1, 0, 0, RewardA), progress.Data, "identity not canonical");
            Require(progress.BankCompletion(run, RewardA) == ProgressActionResult.AlreadyBanked && store.SaveCalls == 1,
                "identity case bypassed once-only deposit");
        });
        Check("failed banking preserves the old ledger and allows a fresh retry", delegate {
            var store = new RecordingStore { FailWrites = true }; var progress = new ClearingProgress(store);
            var run = CompletedRun();
            Require(progress.BankCompletion(run, RewardA) == ProgressActionResult.SaveFailed && progress.SaveFailed,
                "failure not surfaced");
            EqualData(Fresh(), progress.Data, "failed deposit published unsaved coins");
            Require(!progress.IsCompletionBanked(RewardA), "failed deposit claimed banked");
            store.FailWrites = false;
            Require(progress.BankCompletion(run, RewardA) == ProgressActionResult.Saved && !progress.SaveFailed,
                "failed completion could not retry");
            EqualData(Data(1, 0, 0, RewardA), progress.Data, "retry duplicated reward");
        });
        Check("purchases require sufficient saved coins and never spend on rejection", delegate {
            var store = new RecordingStore(); var progress = new ClearingProgress(store);
            Require(progress.Buy(ClearingUpgrade.Focus) == ProgressActionResult.InsufficientCoins, "free upgrade granted");
            EqualData(Fresh(), progress.Data, "rejected purchase changed data");
            Require(store.SaveCalls == 0, "insufficient purchase wrote disk");
        });
        Check("vitality and focus each buy a two-point bonus for thirty coins", delegate {
            foreach (ClearingUpgrade upgrade in new[] { ClearingUpgrade.Vitality, ClearingUpgrade.Focus })
            {
                var progress = new ClearingProgress(new RecordingStore());
                progress.BankCompletion(CompletedRun(), RewardA);
                Require(progress.Buy(upgrade) == ProgressActionResult.Saved, "exact-balance upgrade rejected");
                Require(progress.Data.BankCoins == 0 && progress.Data.Revision == 2, "purchase price or revision wrong");
                Require(upgrade == ClearingUpgrade.Vitality ? progress.BonusHealth == 2 && progress.BonusMana == 0
                    : progress.BonusHealth == 0 && progress.BonusMana == 2, "upgrade affected wrong resource");
            }
        });
        Check("upgrade prices scale thirty forty-five sixty and stop at rank three", delegate {
            var store = new RecordingStore { Stored = Data(10, 0, 0, RewardA), Kind = ProgressLoadKind.Loaded };
            var progress = new ClearingProgress(store);
            foreach (int price in new[] { 30, 45, 60 })
            {
                Require(progress.Price(ClearingUpgrade.Vitality) == price, "price curve differs");
                Require(progress.Buy(ClearingUpgrade.Vitality) == ProgressActionResult.Saved, "funded upgrade rejected");
            }
            EqualData(Data(10, 3, 0, RewardA), progress.Data, "max-rank ledger wrong");
            int saves = store.SaveCalls;
            Require(progress.Buy(ClearingUpgrade.Vitality) == ProgressActionResult.MaxRank
                && progress.BonusHealth == 6 && store.SaveCalls == saves, "rank cap spends or grants excess power");
        });
        Check("failed purchase leaves coins ranks and bonuses unchanged then retries once", delegate {
            var store = new RecordingStore { Stored = Data(2, 0, 0, RewardA), Kind = ProgressLoadKind.Loaded };
            var progress = new ClearingProgress(store); store.FailWrites = true;
            Require(progress.Buy(ClearingUpgrade.Focus) == ProgressActionResult.SaveFailed && progress.SaveFailed,
                "purchase save failure hidden");
            EqualData(Data(2, 0, 0, RewardA), progress.Data, "failed purchase changed resources");
            Require(progress.BonusMana == 0, "unsaved bonus granted");
            store.FailWrites = false;
            Require(progress.Buy(ClearingUpgrade.Focus) == ProgressActionResult.Saved && !progress.SaveFailed,
                "purchase retry failed");
            EqualData(Data(2, 0, 1, RewardA), progress.Data, "retry double-spent");
        });
        Check("unsupported or unavailable persistence blocks writes and purchases", delegate {
            foreach (ProgressLoadKind kind in new[] { ProgressLoadKind.Unavailable, ProgressLoadKind.Unsupported })
            {
                var store = new RecordingStore { Stored = Fresh(), Kind = kind, Writable = false };
                var progress = new ClearingProgress(store);
                Require(!progress.CanWrite && progress.LoadKind == kind, "unavailable store became writable");
                Require(progress.BankCompletion(CompletedRun(), RewardA) == ProgressActionResult.Unavailable
                    && progress.Buy(ClearingUpgrade.Vitality) == ProgressActionResult.Unavailable,
                    "blocked store accepted transaction");
                Require(store.SaveCalls == 0, "blocked store received a save");
            }
        });
        Check("capacity limit rejects another clear without touching stored currency", delegate {
            var store = new RecordingStore { Stored = Data(1000000, 0, 0, RewardA), Kind = ProgressLoadKind.Loaded };
            var progress = new ClearingProgress(store);
            Require(progress.BankCompletion(CompletedRun(), RewardB) == ProgressActionResult.CapacityReached,
                "clear limit overflowed");
            EqualData(store.Stored, progress.Data, "capacity rejection mutated data");
            Require(store.SaveCalls == 0, "capacity rejection wrote");
        });
        Check("unknown upgrade never purchases or grants bonuses", delegate {
            var store = new RecordingStore { Stored = Data(5, 0, 0, RewardA), Kind = ProgressLoadKind.Loaded };
            var progress = new ClearingProgress(store);
            ProgressActionResult result = progress.Buy((ClearingUpgrade)99);
            Require(result != ProgressActionResult.Saved && store.SaveCalls == 0, "unknown upgrade accepted");
            EqualData(store.Stored, progress.Data, "unknown upgrade mutated ledger");
        });
        Check("codec round trips fresh and maximum mixed upgrades", delegate {
            foreach (ClearingProgressData data in new[] { Fresh(), Data(9, 3, 3, RewardA), Data(1000000, 3, 3, RewardB) })
            {
                string encoded = ClearingProgressCodec.Encode(data);
                ProgressLoadResult decoded = ClearingProgressCodec.Decode(encoded);
                Require(decoded.Kind == ProgressLoadKind.Loaded && decoded.CanWrite, "round trip rejected");
                EqualData(data, decoded.Data, "round trip changed ledger");
                Require(ClearingProgressCodec.Encode(decoded.Data) == encoded, "canonical encoding unstable");
            }
        });
        Check("codec remains invariant under a non-English current culture", delegate {
            CultureInfo previous = CultureInfo.CurrentCulture;
            try
            {
                var data = Data(10, 1, 2, RewardA); string baseline = ClearingProgressCodec.Encode(data);
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
                string encoded = ClearingProgressCodec.Encode(data);
                Require(encoded == baseline, "culture changed save representation");
                EqualData(data, ClearingProgressCodec.Decode(encoded).Data, "culture changed parsing");
            }
            finally { CultureInfo.CurrentCulture = previous; }
        });
        Check("codec rejects empty truncated and oversized content", delegate {
            string encoded = ClearingProgressCodec.Encode(Data(1, 0, 0, RewardA));
            foreach (string text in new[] { null, "", encoded.Substring(0, encoded.Length / 2), new string('x', 4097) })
                Rejected(text, "bad length accepted");
        });
        Check("checksum rejects changed reward coins before semantic parsing", delegate {
            string encoded = ClearingProgressCodec.Encode(Data(1, 0, 0, RewardA));
            Rejected(encoded.Replace("bankCoins=30", "bankCoins=60"), "tampered reward accepted");
        });
        Check("codec rejects ledger inflation even with a matching checksum", delegate {
            string encoded = ClearingProgressCodec.Encode(Data(1, 0, 0, RewardA));
            Rejected(Reseal(encoded.Replace("bankCoins=30", "bankCoins=60")), "inflated valid-checksum ledger accepted");
            Rejected(Reseal(encoded.Replace("revision=1", "revision=9")), "revision inconsistent with ledger accepted");
        });
        Check("codec rejects negative out-of-range and noncanonical numbers", delegate {
            string encoded = ClearingProgressCodec.Encode(Data(1, 0, 0, RewardA));
            foreach (string change in new[] { "bankCoins=-1", "bankCoins=2147483648", "bankCoins=030", "bankCoins=+30", "bankCoins=30 " })
                Rejected(Reseal(encoded.Replace("bankCoins=30", change)), "invalid currency number accepted");
            Rejected(Reseal(encoded.Replace("vitalityRank=0", "vitalityRank=4")), "excess rank accepted");
            Rejected(Reseal(encoded.Replace("clearedRuns=1", "clearedRuns=1000001")), "excess clear count accepted");
        });
        Check("codec rejects malformed and missing reward identities", delegate {
            string encoded = ClearingProgressCodec.Encode(Data(1, 0, 0, RewardA));
            foreach (string id in new[] { "", "not-a-guid", new string('g', 32), new string('a', 31) })
                Rejected(Reseal(encoded.Replace("lastRewardId=" + RewardA, "lastRewardId=" + id)), "invalid reward ID accepted");
            Rejected(Reseal(ClearingProgressCodec.Encode(Fresh()).Replace("lastRewardId=\n", "lastRewardId=" + RewardA + "\n")),
                "fresh ledger contains an unearned reward ID");
        });
        Check("codec rejects reordered duplicated and trailing fields", delegate {
            string encoded = ClearingProgressCodec.Encode(Data(1, 0, 0, RewardA));
            Rejected(Reseal(encoded.Replace("revision=1\nbankCoins=30", "bankCoins=30\nrevision=1")), "reordered fields accepted");
            Rejected(Reseal(encoded.Replace("revision=1\n", "revision=1\nrevision=1\n")), "duplicate fields accepted");
            Rejected(encoded + "ignored=1\n", "trailing fields accepted");
        });
        Check("future schema is identified and blocked even when its checksum differs", delegate {
            string future = ClearingProgressCodec.Encode(Data(1, 0, 0, RewardA)).Replace("version=1", "version=2");
            ProgressLoadResult result = ClearingProgressCodec.Decode(future);
            Require(result.Kind == ProgressLoadKind.Unsupported && !result.CanWrite, "future schema treated as writable corruption");
        });
        Check("real empty directory loads a new writable ledger without creating files", delegate {
            InDirectory(delegate(string directory) {
                var store = new FileClearingProgressStore(directory); ProgressLoadResult result = store.Load();
                Require(result.Kind == ProgressLoadKind.New && result.CanWrite, "empty store not writable new");
                EqualData(Fresh(), result.Data, "empty store not fresh");
                Require(!File.Exists(store.PrimaryPath) && !File.Exists(store.BackupPath), "read created a save");
            });
        });
        Check("real first save reloads byte-valid earned coins", delegate {
            InDirectory(delegate(string directory) {
                var progress = new ClearingProgress(new FileClearingProgressStore(directory));
                Require(progress.BankCompletion(CompletedRun(), RewardA) == ProgressActionResult.Saved, "real deposit failed");
                var reloaded = new ClearingProgress(new FileClearingProgressStore(directory));
                EqualData(Data(1, 0, 0, RewardA), reloaded.Data, "real first save did not persist");
                Require(reloaded.LoadKind == ProgressLoadKind.Loaded && reloaded.IsCompletionBanked(RewardA), "reload lost state");
            });
        });
        Check("real replacement keeps prior valid bytes in backup", delegate {
            InDirectory(delegate(string directory) {
                var store = new FileClearingProgressStore(directory); SeedTwoSaves(store);
                EqualData(Data(2, 0, 0, RewardB), store.Load().Data, "replacement not current");
                EqualData(Data(1, 0, 0, RewardA), ClearingProgressCodec.Decode(File.ReadAllText(store.BackupPath)).Data,
                    "backup not prior complete save");
                Require(!File.Exists(StagingPath(store)), "committed staging file remains");
            });
        });
        Check("real purchase survives a fresh store instance with coins and bonus intact", delegate {
            InDirectory(delegate(string directory) {
                var progress = new ClearingProgress(new FileClearingProgressStore(directory));
                progress.BankCompletion(CompletedRun(), RewardA);
                Require(progress.Buy(ClearingUpgrade.Focus) == ProgressActionResult.Saved, "real upgrade failed");
                var reloaded = new ClearingProgress(new FileClearingProgressStore(directory));
                EqualData(Data(1, 0, 1, RewardA), reloaded.Data, "real upgrade lost");
                Require(reloaded.BonusMana == 2 && reloaded.BonusHealth == 0, "reload lost resource bonus");
                Require(reloaded.BankCompletion(CompletedRun(), RewardA) == ProgressActionResult.AlreadyBanked,
                    "reload duplicates spent reward");
            });
        });
        Check("real corrupt primary recovers a verified backup", delegate {
            InDirectory(delegate(string directory) {
                var store = new FileClearingProgressStore(directory); SeedTwoSaves(store);
                File.WriteAllText(store.PrimaryPath, "truncated");
                ProgressLoadResult result = new FileClearingProgressStore(directory).Load();
                Require(result.Kind == ProgressLoadKind.Recovered && result.CanWrite, "backup not recovered");
                EqualData(Data(1, 0, 0, RewardA), result.Data, "recovery selected damaged primary");
            });
        });
        Check("saving recovered progress preserves last good backup instead of rotating corruption", delegate {
            InDirectory(delegate(string directory) {
                var store = new FileClearingProgressStore(directory); SeedTwoSaves(store);
                string goodBackup = File.ReadAllText(store.BackupPath);
                File.WriteAllText(store.PrimaryPath, "broken-primary");
                var recovered = new ClearingProgress(new FileClearingProgressStore(directory));
                Require(recovered.BankCompletion(CompletedRun(), RewardC) == ProgressActionResult.Saved, "recovered save failed");
                Require(File.ReadAllText(store.BackupPath) == goodBackup, "corrupt primary destroyed last good backup");
                EqualData(Data(2, 0, 0, RewardC), new FileClearingProgressStore(directory).Load().Data, "recovered commit lost");
            });
        });
        Check("missing primary can recover a valid backup", delegate {
            InDirectory(delegate(string directory) {
                var store = new FileClearingProgressStore(directory); SeedTwoSaves(store); File.Delete(store.PrimaryPath);
                ProgressLoadResult result = store.Load();
                Require(result.Kind == ProgressLoadKind.Recovered && result.CanWrite, "missing-primary recovery failed");
                EqualData(Data(1, 0, 0, RewardA), result.Data, "missing-primary recovery differs");
            });
        });
        Check("two damaged saves remain untouched and block automatic reset", delegate {
            InDirectory(delegate(string directory) {
                var store = new FileClearingProgressStore(directory);
                File.WriteAllText(store.PrimaryPath, "damaged-primary"); File.WriteAllText(store.BackupPath, "damaged-backup");
                var progress = new ClearingProgress(store);
                Require(!progress.CanWrite && progress.LoadKind == ProgressLoadKind.Unavailable, "two damaged saves reset silently");
                Require(progress.BankCompletion(CompletedRun(), RewardA) == ProgressActionResult.Unavailable, "damaged save overwritten");
                Require(File.ReadAllText(store.PrimaryPath) == "damaged-primary"
                    && File.ReadAllText(store.BackupPath) == "damaged-backup", "damaged evidence changed");
            });
        });
        Check("future primary is preserved despite an older usable backup", delegate {
            InDirectory(delegate(string directory) {
                var store = new FileClearingProgressStore(directory); SeedTwoSaves(store);
                string future = File.ReadAllText(store.PrimaryPath).Replace("version=1", "version=2");
                string backup = File.ReadAllText(store.BackupPath); File.WriteAllText(store.PrimaryPath, future);
                ProgressLoadResult result = store.Load();
                Require(result.Kind == ProgressLoadKind.Unsupported && !result.CanWrite, "future primary downgraded to old backup");
                Require(!store.TrySave(Data(1, 0, 0, RewardA), Data(2, 0, 0, RewardC)), "future schema overwritten");
                Require(File.ReadAllText(store.PrimaryPath) == future && File.ReadAllText(store.BackupPath) == backup,
                    "future-schema save files changed");
            });
        });
        Check("future backup also blocks downgrade writes while preserving both files", delegate {
            InDirectory(delegate(string directory) {
                var store = new FileClearingProgressStore(directory); SeedTwoSaves(store);
                string primary = File.ReadAllText(store.PrimaryPath);
                string future = File.ReadAllText(store.BackupPath).Replace("version=1", "version=2");
                File.WriteAllText(store.BackupPath, future);
                ProgressLoadResult result = store.Load();
                Require(result.Kind == ProgressLoadKind.Unsupported && !result.CanWrite, "future backup ignored");
                Require(!store.TrySave(Data(2, 0, 0, RewardB), Data(3, 0, 0, RewardC)), "future backup overwritten");
                Require(File.ReadAllText(store.PrimaryPath) == primary && File.ReadAllText(store.BackupPath) == future,
                    "future backup protection changed save files");
            });
        });
        Check("real invalid UTF-8 and oversized primary data remain unavailable", delegate {
            InDirectory(delegate(string directory) {
                var store = new FileClearingProgressStore(directory);
                foreach (byte[] bytes in new[] { new byte[] { 0xff, 0xfe, 0xfd }, new byte[4097] })
                {
                    File.WriteAllBytes(store.PrimaryPath, bytes);
                    ProgressLoadResult result = store.Load();
                    Require(result.Kind == ProgressLoadKind.Unavailable && !result.CanWrite, "invalid file became fresh");
                    Require(File.ReadAllBytes(store.PrimaryPath).Length == bytes.Length, "invalid file changed during load");
                }
            });
        });
        Check("orphan staging data never supersedes the committed save", delegate {
            InDirectory(delegate(string directory) {
                var store = new FileClearingProgressStore(directory);
                File.WriteAllText(StagingPath(store), ClearingProgressCodec.Encode(Data(1, 0, 0, RewardA)));
                Require(store.Load().Kind == ProgressLoadKind.New && store.Load().Data.BankCoins == 0, "orphan stage granted coins");
                Require(store.TrySave(Fresh(), Data(1, 0, 0, RewardB)), "orphan stage prevented legitimate commit");
                EqualData(Data(1, 0, 0, RewardB), store.Load().Data, "orphan reward supplanted new commit");
            });
        });
        Check("real staging failure leaves memory unchanged and can retry after removing the fault", delegate {
            InDirectory(delegate(string directory) {
                var store = new FileClearingProgressStore(directory); Directory.CreateDirectory(StagingPath(store));
                var progress = new ClearingProgress(store); var run = CompletedRun();
                Require(progress.BankCompletion(run, RewardA) == ProgressActionResult.SaveFailed && progress.SaveFailed,
                    "staging IO failure hidden");
                EqualData(Fresh(), progress.Data, "staging failure published coins");
                Require(!File.Exists(store.PrimaryPath), "staging failure published a primary file");
                Directory.Delete(StagingPath(store));
                Require(progress.BankCompletion(run, RewardA) == ProgressActionResult.Saved, "staging failure was not retryable");
                EqualData(Data(1, 0, 0, RewardA), store.Load().Data, "retry corrupted earned reward");
            });
        });
        Check("real backup path collision preserves committed bytes and retries without duplicate reward", delegate {
            InDirectory(delegate(string directory) {
                var store = new FileClearingProgressStore(directory);
                Require(store.TrySave(Fresh(), Data(1, 0, 0, RewardA)), "replace failure setup save failed");
                string committed = File.ReadAllText(store.PrimaryPath);
                var progress = new ClearingProgress(store); var run = CompletedRun();
                Directory.CreateDirectory(store.BackupPath);
                Require(progress.BankCompletion(run, RewardB) == ProgressActionResult.SaveFailed, "replace failure hidden");
                EqualData(Data(1, 0, 0, RewardA), progress.Data, "failed replacement published coins");
                Require(File.ReadAllText(store.PrimaryPath) == committed, "failed replace changed last committed save");
                Directory.Delete(store.BackupPath);
                Require(progress.BankCompletion(run, RewardB) == ProgressActionResult.Saved, "replacement could not retry");
                EqualData(Data(2, 0, 0, RewardB), store.Load().Data, "replace retry duplicated reward");
            });
        });
        Check("real exclusive-lock contention rejects a save then permits a later retry", delegate {
            InDirectory(delegate(string directory) {
                var store = new FileClearingProgressStore(directory); var progress = new ClearingProgress(store);
                var run = CompletedRun();
                using (var guard = new FileStream(Path.ChangeExtension(store.PrimaryPath, ".lock"),
                    FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                {
                    Require(progress.BankCompletion(run, RewardA) == ProgressActionResult.SaveFailed,
                        "contending writer acquired an exclusive lock");
                    EqualData(Fresh(), progress.Data, "lock failure published coins");
                    Require(!File.Exists(store.PrimaryPath), "lock failure created a primary");
                }
                Require(progress.BankCompletion(run, RewardA) == ProgressActionResult.Saved, "lock release did not allow retry");
                EqualData(Data(1, 0, 0, RewardA), store.Load().Data, "lock retry ledger wrong");
            });
        });
        Check("real staging fault preserves both the latest save and prior backup", delegate {
            InDirectory(delegate(string directory) {
                var store = new FileClearingProgressStore(directory); SeedTwoSaves(store);
                string primary = File.ReadAllText(store.PrimaryPath), backup = File.ReadAllText(store.BackupPath);
                var progress = new ClearingProgress(store);
                Directory.CreateDirectory(StagingPath(store));
                Require(progress.Buy(ClearingUpgrade.Focus) == ProgressActionResult.SaveFailed,
                    "later staging fault hidden");
                EqualData(Data(2, 0, 0, RewardB), progress.Data, "failed staged purchase granted unsaved bonus");
                Require(File.ReadAllText(store.PrimaryPath) == primary && File.ReadAllText(store.BackupPath) == backup,
                    "staging failure damaged committed save or backup");
            });
        });
        Check("real store rejects skipped revisions and same-revision divergent snapshots", delegate {
            InDirectory(delegate(string directory) {
                var store = new FileClearingProgressStore(directory);
                Require(store.TrySave(Fresh(), Data(1, 0, 0, RewardA)), "transition setup save failed");
                string primary = File.ReadAllText(store.PrimaryPath);
                Require(!store.TrySave(Data(1, 0, 0, RewardA), Data(3, 0, 0, RewardC)), "skipped revision accepted");
                Require(!store.TrySave(Data(1, 0, 0, RewardB), Data(2, 0, 0, RewardC)), "divergent same-revision expected ledger accepted");
                Require(!store.TrySave(null, Data(2, 0, 0, RewardB)) && !store.TrySave(Data(1, 0, 0, RewardA), null),
                    "missing transaction data accepted");
                Require(File.ReadAllText(store.PrimaryPath) == primary, "invalid transaction changed committed bytes");
            });
        });
        Check("real stale writer is rejected without overwriting a newer purchase", delegate {
            InDirectory(delegate(string directory) {
                var first = new ClearingProgress(new FileClearingProgressStore(directory));
                first.BankCompletion(CompletedRun(), RewardA);
                var stale = new ClearingProgress(new FileClearingProgressStore(directory));
                Require(first.Buy(ClearingUpgrade.Vitality) == ProgressActionResult.Saved, "newer purchase failed");
                var store = new FileClearingProgressStore(directory); string current = File.ReadAllText(store.PrimaryPath);
                Require(stale.BankCompletion(CompletedRun(), RewardB) == ProgressActionResult.SaveFailed,
                    "stale snapshot overwrote newer save");
                Require(File.ReadAllText(store.PrimaryPath) == current, "stale writer changed committed bytes");
                EqualData(Data(1, 1, 0, RewardA), store.Load().Data, "stale writer erased upgrade");
                EqualData(Data(1, 0, 0, RewardA), stale.Data, "failed stale transaction published new coins");
            });
        });
        Check("real path collision reports unavailable without replacing unrelated data", delegate {
            InDirectory(delegate(string directory) {
                string collision = Path.Combine(directory, "not-a-directory"); File.WriteAllText(collision, "keep-me");
                var progress = new ClearingProgress(new FileClearingProgressStore(collision));
                Require(!progress.CanWrite && progress.LoadKind == ProgressLoadKind.Unavailable, "file path accepted as save directory");
                Require(progress.BankCompletion(CompletedRun(), RewardA) == ProgressActionResult.Unavailable,
                    "path collision transaction accepted");
                Require(File.ReadAllText(collision) == "keep-me", "path collision destroyed unrelated data");
            });
        });
        Console.WriteLine("Progression: " + passed + " passed, " + failed + " failed; actual pure C# and real temporary filesystem IO.");
        return failed == 0 ? 0 : 1;
    }
}
