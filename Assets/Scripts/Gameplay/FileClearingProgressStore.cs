using System;
using System.IO;
using System.Security;
using System.Text;

namespace Rpg.Gameplay
{
    /// <summary>
    /// Desktop candidate save store. Cooperating instances share an exclusive lock;
    /// compare-before-replace refuses stale writers. No unsafe delete-and-move fallback.
    /// </summary>
    public sealed class FileClearingProgressStore : IClearingProgressStore
    {
        private readonly string directory;
        private readonly string temporaryPath;
        private readonly string lockPath;
        public string PrimaryPath { get; private set; }
        public string BackupPath { get; private set; }

        public FileClearingProgressStore(string directory)
        {
            if (string.IsNullOrEmpty(directory)) throw new ArgumentException("A save directory is required.", "directory");
            this.directory = Path.GetFullPath(directory);
            PrimaryPath = Path.Combine(this.directory, "clearing-progress.dat");
            BackupPath = Path.Combine(this.directory, "clearing-progress.bak");
            temporaryPath = Path.Combine(this.directory, "clearing-progress.tmp");
            lockPath = Path.Combine(this.directory, "clearing-progress.lock");
        }

        public ProgressLoadResult Load()
        {
            try
            {
                Directory.CreateDirectory(directory);
                using (FileStream guard = Lock()) return ReadCurrent();
            }
            catch (IOException) { return Unavailable(); }
            catch (UnauthorizedAccessException) { return Unavailable(); }
            catch (SecurityException) { return Unavailable(); }
            catch (NotSupportedException) { return Unavailable(); }
        }

        public bool TrySave(ClearingProgressData expected, ClearingProgressData next)
        {
            if (expected == null || next == null || next.Revision != expected.Revision + 1) return false;
            try
            {
                Directory.CreateDirectory(directory);
                using (FileStream guard = Lock())
                {
                    ProgressLoadResult current = ReadCurrent();
                    if (!current.CanWrite || !current.Data.SameAs(expected)) return false;
                    byte[] bytes = ClearingProgressCodec.Utf8.GetBytes(ClearingProgressCodec.Encode(next));
                    using (FileStream staged = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        staged.Write(bytes, 0, bytes.Length);
                        staged.Flush(true);
                    }
                    if (!File.Exists(PrimaryPath)) File.Move(temporaryPath, PrimaryPath);
                    else if (current.Kind == ProgressLoadKind.Recovered)
                    {
                        // Do not rotate a damaged primary over the known-good backup.
                        string rejected = PrimaryPath + ".rejected-" + Guid.NewGuid().ToString("N");
                        File.Replace(temporaryPath, PrimaryPath, rejected);
                    }
                    else File.Replace(temporaryPath, PrimaryPath, BackupPath);
                    return true;
                }
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
            catch (SecurityException) { return false; }
            catch (NotSupportedException) { return false; }
        }

        private FileStream Lock()
        {
            return new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }

        private ProgressLoadResult ReadCurrent()
        {
            bool primaryExists = File.Exists(PrimaryPath);
            bool backupExists = File.Exists(BackupPath);
            // Directories at data paths are an unavailable store, never a fresh slot.
            if (Directory.Exists(PrimaryPath) || Directory.Exists(BackupPath)) return Unavailable();
            ProgressLoadResult primary = primaryExists ? Read(PrimaryPath) : null;
            ProgressLoadResult backup = backupExists ? Read(BackupPath) : null;
            if ((primary != null && primary.Kind == ProgressLoadKind.Unsupported)
                || (backup != null && backup.Kind == ProgressLoadKind.Unsupported))
                return ClearingProgressCodec.Invalid(ProgressLoadKind.Unsupported);
            if (primary != null && primary.CanWrite) return primary;
            if (backup != null && backup.CanWrite)
                return new ProgressLoadResult(backup.Data, ProgressLoadKind.Recovered, true);
            if (!primaryExists && !backupExists)
                return new ProgressLoadResult(ClearingProgressData.Fresh, ProgressLoadKind.New, true);
            return Unavailable();
        }

        private static ProgressLoadResult Read(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length > ClearingProgressCodec.MaximumBytes) return Unavailable();
                byte[] bytes = new byte[(int)stream.Length];
                int read = 0;
                while (read < bytes.Length)
                {
                    int count = stream.Read(bytes, read, bytes.Length - read);
                    if (count == 0) return Unavailable();
                    read += count;
                }
                try { return ClearingProgressCodec.Decode(ClearingProgressCodec.Utf8.GetString(bytes)); }
                catch (DecoderFallbackException) { return Unavailable(); }
            }
        }

        private static ProgressLoadResult Unavailable()
        {
            return ClearingProgressCodec.Invalid(ProgressLoadKind.Unavailable);
        }
    }
}
