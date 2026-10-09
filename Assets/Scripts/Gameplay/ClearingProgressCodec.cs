using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Rpg.Gameplay
{
    /// <summary>
    /// Strict UTF-8 v1 text: magic, version, six ordered ledger fields, then a
    /// lowercase SHA-256 of all preceding lines including their LF terminators.
    /// The checksum detects damage; it is not encryption or anti-cheat protection.
    /// </summary>
    public static class ClearingProgressCodec
    {
        public const int MaximumBytes = 4096;
        internal const int MaximumHeaderBytes = 64;
        private const string Magic = "RPG-CLEARING-PROGRESS";
        internal static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);

        public static string Encode(ClearingProgressData data)
        {
            if (data == null) throw new ArgumentNullException("data");
            string body = Magic + "\nversion=1\nrevision=" + Number(data.Revision)
                + "\nbankCoins=" + Number(data.BankCoins) + "\nclearedRuns=" + Number(data.ClearedRuns)
                + "\nvitalityRank=" + Number(data.VitalityRank) + "\nfocusRank=" + Number(data.FocusRank)
                + "\nlastRewardId=" + data.LastRewardId + "\n";
            return body + "checksum=" + Checksum(body) + "\n";
        }

        public static ProgressLoadResult Decode(string text)
        {
            // Protect a recognizable newer schema before validating its unknown
            // payload. A newer writer may legitimately use a larger body, and
            // later damage must never make that file eligible for v1 recovery.
            if (HasFutureVersion(text)) return Invalid(ProgressLoadKind.Unsupported);
            if (text == null || text.Length > MaximumBytes) return Invalid(ProgressLoadKind.Unavailable);
            try
            {
                if (Utf8.GetByteCount(text) > MaximumBytes) return Invalid(ProgressLoadKind.Unavailable);
                string[] lines = text.Split('\n');
                int version;
                if (lines.Length < 2 || lines[0] != Magic || !TryNumber(lines[1], "version=", out version))
                    return Invalid(ProgressLoadKind.Unavailable);
                if (version != 1 || lines.Length != 10 || lines[9] != "") return Invalid(ProgressLoadKind.Unavailable);
                int revision, coins, clears, vitality, focus;
                if (!TryNumber(lines[2], "revision=", out revision) || !TryNumber(lines[3], "bankCoins=", out coins)
                    || !TryNumber(lines[4], "clearedRuns=", out clears) || !TryNumber(lines[5], "vitalityRank=", out vitality)
                    || !TryNumber(lines[6], "focusRank=", out focus) || !lines[7].StartsWith("lastRewardId=", StringComparison.Ordinal))
                    return Invalid(ProgressLoadKind.Unavailable);
                string rewardId = lines[7].Substring("lastRewardId=".Length);
                ClearingProgressData data = new ClearingProgressData(revision, coins, clears, vitality, focus, rewardId);
                // Canonical reconstruction also rejects extra fields, whitespace, uppercase
                // IDs/checksums, duplicate fields, CRLF and noncanonical numeric spellings.
                if (Encode(data) != text) return Invalid(ProgressLoadKind.Unavailable);
                return new ProgressLoadResult(data, ProgressLoadKind.Loaded, true);
            }
            catch (ArgumentException) { return Invalid(ProgressLoadKind.Unavailable); }
        }

        internal static ProgressLoadResult Invalid(ProgressLoadKind kind)
        {
            return new ProgressLoadResult(ClearingProgressData.Fresh, kind, false);
        }

        internal static bool HasFutureVersion(string text)
        {
            const string prefix = Magic + "\nversion=";
            if (text == null || !text.StartsWith(prefix, StringComparison.Ordinal)) return false;
            int end = Math.Min(text.Length, MaximumHeaderBytes);
            int digits = 0;
            char first = '\0';
            for (int i = prefix.Length; i < end; i++)
            {
                char c = text[i];
                if (c == '\n') break;
                if (c < '0' || c > '9') return false;
                if (digits == 0) first = c;
                digits++;
            }
            // Any canonical multi-digit version is newer than v1; inspecting
            // digits avoids overflow when a future version exceeds Int32.
            return digits > 0 && first != '0' && (digits > 1 || first > '1');
        }

        private static bool TryNumber(string line, string prefix, out int value)
        {
            value = 0;
            if (!line.StartsWith(prefix, StringComparison.Ordinal)) return false;
            string digits = line.Substring(prefix.Length);
            return digits.Length > 0 && !(digits.Length > 1 && digits[0] == '0')
                && int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out value);
        }

        private static string Number(int value) { return value.ToString(CultureInfo.InvariantCulture); }
        private static string Checksum(string body)
        {
            using (SHA256 hash = SHA256.Create())
            {
                byte[] digest = hash.ComputeHash(Utf8.GetBytes(body));
                StringBuilder hex = new StringBuilder(64);
                foreach (byte value in digest) hex.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return hex.ToString();
            }
        }
    }
}
