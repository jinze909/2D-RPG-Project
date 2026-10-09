using System;
using Rpg.Gameplay;

internal static class ClearingSuppliesChecks
{
    private static int passed, failed;
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Test(string name, Action body)
    {
        try { body(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + ": " + error.Message); }
    }
    private static ClearingSupplies Found(int index)
    {
        var value = new ClearingSupplies();
        Check(value.TryDiscover(index, 0f, true, true), "setup discovery rejected");
        return value;
    }
    private static void Claim(ClearingSupplies supplies, int index, float distance, bool clear, bool playable,
        float hp, float hpMax, float mp, float mpMax, SupplyClaimResult expected, float expectedGain)
    {
        float gain;
        var result = supplies.TryClaim(index, distance, clear, playable, hp, hpMax, mp, mpMax, out gain);
        Check(result == expected, "expected " + expected + ", got " + result);
        Check(gain == expectedGain, "expected gain " + expectedGain + ", got " + gain);
    }
    private static void InvalidStats(float hp, float hpMax, float mp, float mpMax)
    {
        for (int id = 0; id < 2; id++)
        {
            var supplies = Found(id);
            Claim(supplies, id, 0f, true, true, hp, hpMax, mp, mpMax, SupplyClaimResult.Invalid, 0f);
            Check(!supplies.IsClaimed(id), "invalid/dead state spent a reserve");
        }
    }
    public static int Main()
    {
        Test("fresh state and authored feet positions", () =>
        {
            var s = new ClearingSupplies();
            Check(ClearingSupplies.Count == 2 && ClearingSupplies.PositionX(0) == -5.6f &&
                ClearingSupplies.PositionY(0) == -2.35f && ClearingSupplies.PositionX(1) == 5.6f &&
                ClearingSupplies.PositionY(1) == 1.15f, "wrong authored feet positions");
            for (int id = -1; id <= 2; id++) Check(!s.IsDiscovered(id) && !s.IsClaimed(id), "fresh/invalid flags");
            bool badX = false, badY = false;
            try { ClearingSupplies.PositionX(-1); } catch (ArgumentOutOfRangeException) { badX = true; }
            try { ClearingSupplies.PositionY(2); } catch (ArgumentOutOfRangeException) { badY = true; }
            Check(badX && badY, "invalid coordinates did not reject");
        });
        Test("inclusive discovery boundary and one event", () =>
        {
            var s = new ClearingSupplies();
            Check(!s.TryDiscover(0, 2.4001f, true, true), "outside discovery radius");
            Check(s.TryDiscover(0, 2.4f, true, true) && !s.TryDiscover(0, 0f, true, true), "boundary/one event");
            Check(!s.IsDiscovered(1) && s.TryDiscover(1, 0f, true, true), "independent discovery");
            Check(!s.IsClaimed(0) && !s.IsClaimed(1), "discovery consumed reserves");
        });
        Test("blocked and inactive discovery preserve hidden and remembered state", () =>
        {
            var s = new ClearingSupplies();
            Check(!s.TryDiscover(0, 1f, false, true) && !s.TryDiscover(0, 1f, true, false) &&
                !s.IsDiscovered(0), "LOS/playable gate");
            Check(s.TryDiscover(0, 1f, true, true) && !s.TryDiscover(0, 1f, false, false) &&
                s.IsDiscovered(0), "discovery forgotten on later blocked observation");
        });
        Test("claim discovery playability range and LOS gates retain charge", () =>
        {
            var s = new ClearingSupplies();
            Claim(s, 0, 0f, true, true, 10, 20, 0, 12, SupplyClaimResult.Undiscovered, 0);
            Check(s.TryDiscover(0, 1f, true, true), "discovery");
            Claim(s, 0, 0f, true, false, 10, 20, 0, 12, SupplyClaimResult.Inactive, 0);
            Claim(s, 0, .8001f, true, true, 10, 20, 0, 12, SupplyClaimResult.OutOfRange, 0);
            Claim(s, 0, .8f, false, true, 10, 20, 0, 12, SupplyClaimResult.Blocked, 0);
            Check(!s.IsClaimed(0), "rejected claim spent");
            Claim(s, 0, .8f, true, true, 10, 20, 0, 12, SupplyClaimResult.Restored, 4);
        });
        Test("herbs restore four HP and leave rune unchanged", () =>
        {
            var s = Found(0);
            Claim(s, 0, .5f, true, true, 18, 30, 5, 12, SupplyClaimResult.Restored, 4);
            Check(s.IsDiscovered(0) && s.IsClaimed(0) && !s.IsDiscovered(1) && !s.IsClaimed(1), "herb lifecycle");
        });
        Test("rune restores six MP and leaves herbs unchanged", () =>
        {
            var s = Found(1);
            Claim(s, 1, .5f, true, true, 20, 20, 3, 12, SupplyClaimResult.Restored, 6);
            Check(s.IsDiscovered(1) && s.IsClaimed(1) && !s.IsClaimed(0), "rune lifecycle");
        });
        Test("actual gains clamp to ordinary and grown capacities", () =>
        {
            Claim(Found(0), 0, 0, true, true, 19, 20, 0, 12, SupplyClaimResult.Restored, 1);
            Claim(Found(0), 0, 0, true, true, 17.5f, 20, 0, 12, SupplyClaimResult.Restored, 2.5f);
            Claim(Found(0), 0, 0, true, true, 97, 100, 0, 30, SupplyClaimResult.Restored, 3);
            Claim(Found(1), 1, 0, true, true, 20, 20, 9, 12, SupplyClaimResult.Restored, 3);
            Claim(Found(1), 1, 0, true, true, 20, 20, 9.75f, 12, SupplyClaimResult.Restored, 2.25f);
            Claim(Found(1), 1, 0, true, true, 40, 40, 27, 30, SupplyClaimResult.Restored, 3);
        });
        Test("full resources and zero mana capacity retain charge", () =>
        {
            var hp = Found(0);
            Claim(hp, 0, 0, true, true, 20, 20, 0, 12, SupplyClaimResult.Full, 0);
            Check(!hp.IsClaimed(0), "full health spent");
            Claim(hp, 0, 0, true, true, 16, 20, 0, 12, SupplyClaimResult.Restored, 4);
            var mp = Found(1);
            Claim(mp, 1, 0, true, true, 20, 20, 12, 12, SupplyClaimResult.Full, 0);
            Check(!mp.IsClaimed(1), "full mana spent");
            Claim(mp, 1, 0, true, true, 20, 20, 6, 12, SupplyClaimResult.Restored, 6);
            var zero = Found(1);
            Claim(zero, 1, 0, true, true, 20, 20, 0, 0, SupplyClaimResult.Full, 0);
            Check(!zero.IsClaimed(1), "zero mana capacity spent");
        });
        Test("repeated use or observation cannot restock a spent reserve", () =>
        {
            var s = Found(0);
            Claim(s, 0, 0, true, true, 10, 20, 0, 12, SupplyClaimResult.Restored, 4);
            Claim(s, 0, 0, true, true, 5, 20, 0, 12, SupplyClaimResult.Spent, 0);
            Claim(s, 0, 5, false, true, 5, 20, 0, 12, SupplyClaimResult.Spent, 0);
            Check(!s.TryDiscover(0, 0, true, true) && s.IsClaimed(0), "rediscovery restocked");
        });
        Test("independent charges restock only on explicit new-run Reset", () =>
        {
            var s = Found(0); Check(s.TryDiscover(1, 0, true, true), "rune setup");
            Claim(s, 0, 0, true, true, 10, 20, 0, 12, SupplyClaimResult.Restored, 4);
            Check(!s.IsClaimed(1), "herbs spent rune");
            Claim(s, 1, 0, true, true, 14, 20, 0, 12, SupplyClaimResult.Restored, 6);
            s.Reset();
            for (int id = 0; id < 2; id++) Check(!s.IsDiscovered(id) && !s.IsClaimed(id), "new run did not reset");
            Claim(s, 0, 0, true, true, 10, 20, 0, 12, SupplyClaimResult.Undiscovered, 0);
            Check(s.TryDiscover(0, 0, true, true), "new run discovery rejected");
            Claim(s, 0, 0, true, true, 10, 20, 0, 12, SupplyClaimResult.Restored, 4);
        });
        Test("invalid identifiers and geometry preserve charges", () =>
        {
            var s = Found(0);
            foreach (int id in new[] { -1, 2, int.MinValue, int.MaxValue })
            {
                Check(!s.TryDiscover(id, 0, true, true) && !s.IsDiscovered(id) && !s.IsClaimed(id), "invalid identifier");
                Claim(s, id, 0, true, true, 10, 20, 0, 12, SupplyClaimResult.Invalid, 0);
            }
            foreach (float distance in new[] { -.001f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                Check(!s.TryDiscover(1, distance, true, true), "invalid distance discovered");
                Claim(s, 0, distance, true, true, 10, 20, 0, 12, SupplyClaimResult.Invalid, 0);
            }
            Check(!s.IsClaimed(0), "invalid geometry spent reserve");
        });
        Test("nonfinite dead negative and overfilled resources never restore", () =>
        {
            foreach (float value in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                InvalidStats(value, 20, 0, 12); InvalidStats(10, value, 0, 12);
                InvalidStats(10, 20, value, 12); InvalidStats(10, 20, 0, value);
            }
            InvalidStats(0, 20, 0, 12); InvalidStats(-1, 20, 0, 12); InvalidStats(21, 20, 0, 12);
            InvalidStats(10, 0, 0, 12); InvalidStats(10, -1, 0, 12);
            InvalidStats(10, 20, -1, 12); InvalidStats(10, 20, 13, 12); InvalidStats(10, 20, 0, -1);
        });
        Test("representable float gains cannot exceed or silently waste the fixed budget", () =>
        {
            var hp = Found(0);
            Claim(hp, 0, 0, true, true, 134217728f, 134217744f, 0, 12, SupplyClaimResult.Full, 0);
            Check(!hp.IsClaimed(0), "zero effective gain spent herbs");
            Claim(hp, 0, 0, true, true, 67108872f, 67108896f, 0, 12, SupplyClaimResult.Invalid, 0);
            Check(!hp.IsClaimed(0), "four HP rounded into eight");
            Claim(hp, 0, 0, true, true, 10, 20, 0, 12, SupplyClaimResult.Restored, 4);
            var mp = Found(1);
            Claim(mp, 1, 0, true, true, 20, 20, 134217728f, 134217744f, SupplyClaimResult.Full, 0);
            Check(!mp.IsClaimed(1), "zero effective gain spent rune");
            Claim(mp, 1, 0, true, true, 20, 20, 67108864f, 67108896f, SupplyClaimResult.Invalid, 0);
            Check(!mp.IsClaimed(1), "six MP rounded into eight");
            Claim(Found(0), 0, 0, true, true, .1f, .2f, 0, 12, SupplyClaimResult.Restored, .2f - .1f);
            Claim(mp, 1, 0, true, true, 20, 20, .1f, .2f, SupplyClaimResult.Restored, .2f - .1f);
        });
        Console.WriteLine("RESULT " + passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
}
