namespace SprocketMaterialSelector;

internal enum ConfigMigrationResult { CanonicalExists, NoLegacyFile, Copied }

internal static class ConfigMigration
{
    internal static ConfigMigrationResult CopyLegacyIfNeeded(string legacyPath, string canonicalPath)
    {
        if (File.Exists(canonicalPath)) return ConfigMigrationResult.CanonicalExists;
        if (!File.Exists(legacyPath)) return ConfigMigrationResult.NoLegacyFile;
        var target = Path.GetFullPath(canonicalPath);
        var directory = Path.GetDirectoryName(target) ?? throw new IOException("Configuration directory unavailable.");
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, Path.GetFileName(target) + ".migration-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.Copy(legacyPath, temporary, overwrite:false);
            try { File.Move(temporary, target); }
            catch (IOException) when (File.Exists(target)) { return ConfigMigrationResult.CanonicalExists; }
            return ConfigMigrationResult.Copied;
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
