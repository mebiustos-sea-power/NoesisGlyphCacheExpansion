using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

internal static class Program
{
    private static int Main()
    {
        try
        {
            CacheChangesOnlyTheSize();
            CacheRejectsMissingSettings();
            BoundaryAllowsHealthyOperations();
            BoundaryContainsInitializationFailure();
            BoundaryContainsMissingSettings();
            ActivationRespectsCheckboxAndFolder();
            ActivationReportsMissingState();
            Console.WriteLine("PASS: cache override, preserved settings, repeat application, checkbox states, path matching, activation errors and failure isolation.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    private static void CacheChangesOnlyTheSize()
    {
        // Allocate managed fields without a Unity constructor or any native calls.
        var settings = (NoesisSettings)RuntimeHelpers.GetUninitializedObject(typeof(NoesisSettings));
        settings.glyphTextureSize = NoesisSettings.TextureSize._2048x2048;
        settings.offscreenInitSurfaces = 3;
        settings.offscreenMaxSurfaces = 7;
        settings.defaultFontSize = 23;
        settings.loadPlatformFonts = true;
        FieldInfo[] fields = typeof(NoesisSettings).GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(field => field.Name != "glyphTextureSize").ToArray();
        object[] originals = fields.Select(field => field.GetValue(settings)).ToArray();
        var previous = NoesisGlyphCacheExpansion.CacheSizeOverride.Apply(settings);
        Require(previous == NoesisSettings.TextureSize._2048x2048, "The previous cache size must be reported.");
        Require(settings.glyphTextureSize == NoesisSettings.TextureSize._4096x4096, "The requested cache size must be applied.");
        for (int index = 0; index < fields.Length; index++)
            Require(Equals(fields[index].GetValue(settings), originals[index]), "Unrelated setting changed: " + fields[index].Name);
        previous = NoesisGlyphCacheExpansion.CacheSizeOverride.Apply(settings);
        Require(previous == NoesisSettings.TextureSize._4096x4096 &&
                settings.glyphTextureSize == NoesisSettings.TextureSize._4096x4096, "Repeated reads must preserve the requested size.");
    }

    private static void CacheRejectsMissingSettings()
    {
        RequireThrows<InvalidOperationException>(() => NoesisGlyphCacheExpansion.CacheSizeOverride.Apply(null));
    }

    private static void BoundaryAllowsHealthyOperations()
    {
        var errors = new List<string>();
        var boundary = new NoesisGlyphCacheExpansion.FailureBoundary(errors.Add);
        var settings = (NoesisSettings)RuntimeHelpers.GetUninitializedObject(typeof(NoesisSettings));
        settings.glyphTextureSize = NoesisSettings.TextureSize._2048x2048;
        int completed = 0;
        boundary.Run("Initialization", () => completed++);
        boundary.Run("Glyph cache override", () =>
        {
            NoesisGlyphCacheExpansion.CacheSizeOverride.Apply(settings);
            completed++;
        });
        boundary.Run("Glyph cache override", () =>
        {
            NoesisGlyphCacheExpansion.CacheSizeOverride.Apply(settings);
            completed++;
        });
        Require(completed == 3 && errors.Count == 0, "Healthy operations must continue without error reports.");
        Require(settings.glyphTextureSize == NoesisSettings.TextureSize._4096x4096,
            "A healthy guarded override must still expand the cache.");
    }

    private static void BoundaryContainsInitializationFailure()
    {
        var errors = new List<string>();
        var boundary = new NoesisGlyphCacheExpansion.FailureBoundary(errors.Add);
        var failure = new MissingMethodException("Simulated unavailable settings getter.");
        int attempts = 0;
        Action initialize = () =>
        {
            attempts++;
            throw failure;
        };
        boundary.Run("Initialization", initialize);
        boundary.Run("Initialization", initialize);
        boundary.Run("Glyph cache override", () => attempts++);
        Require(attempts == 1, "Failed initialization must stop subsequent initialization and override work.");
        Require(errors.Count == 1 && errors[0].Contains("Initialization") && errors[0].Contains(failure.ToString()),
            "Initialization failure must report its original exception once without escaping to the loader.");
    }

    private static void BoundaryContainsMissingSettings()
    {
        var errors = new List<string>();
        var boundary = new NoesisGlyphCacheExpansion.FailureBoundary(errors.Add);
        int attempts = 0;
        for (int index = 0; index < 3; index++)
        {
            boundary.Run("Glyph cache override", () =>
            {
                attempts++;
                NoesisGlyphCacheExpansion.CacheSizeOverride.Apply(null);
            });
        }
        var settings = (NoesisSettings)RuntimeHelpers.GetUninitializedObject(typeof(NoesisSettings));
        settings.glyphTextureSize = NoesisSettings.TextureSize._2048x2048;
        boundary.Run("Glyph cache override", () => NoesisGlyphCacheExpansion.CacheSizeOverride.Apply(settings));
        Require(attempts == 1 && settings.glyphTextureSize == NoesisSettings.TextureSize._2048x2048,
            "A failed callback must stop further cache changes for the session.");
        Require(errors.Count == 1 && errors[0].Contains(nameof(InvalidOperationException)) &&
                errors[0].Contains("Noesis settings asset is missing"),
            "Missing settings must report the cause once without escaping to the game.");
    }

    private static void ActivationRespectsCheckboxAndFolder()
    {
        string package = Path.Combine(Path.GetTempPath(), "NoesisGlyphCacheExpansion-TestPackage");
        var entry = new FakeDirectory { DirectoryInfo = new DirectoryInfo(package), IsChecked = true };
        FakeManager.Instance = new FakeManager
        {
            Directories = new[]
            {
                new FakeDirectory { DirectoryInfo = new DirectoryInfo(package + "-other"), IsChecked = false },
                entry
            }
        };
        Require(NoesisGlyphCacheExpansion.ModActivation.IsEnabled(typeof(FakeManager), package + Path.DirectorySeparatorChar),
            "An enabled package must match independently of a trailing separator.");
        Require(NoesisGlyphCacheExpansion.ModActivation.IsEnabled(typeof(FakeManager), package.ToUpperInvariant()),
            "Windows mod paths must match without case sensitivity.");
        entry.IsChecked = false;
        Require(!NoesisGlyphCacheExpansion.ModActivation.IsEnabled(typeof(FakeManager), package),
            "An unchecked package must not be treated as enabled.");
        RequireThrows<InvalidOperationException>(() =>
            NoesisGlyphCacheExpansion.ModActivation.IsEnabled(typeof(FakeManager), package + "-missing"));
    }

    private static void ActivationReportsMissingState()
    {
        string package = Path.Combine(Path.GetTempPath(), "NoesisGlyphCacheExpansion-TestPackage");
        FakeManager.Instance = null;
        RequireThrows<InvalidOperationException>(() => NoesisGlyphCacheExpansion.ModActivation.IsEnabled(typeof(FakeManager), package));
        RequireThrows<MissingMemberException>(() => NoesisGlyphCacheExpansion.ModActivation.IsEnabled(typeof(object), package));
        FakeManager.Instance = new FakeManager { Directories = null };
        RequireThrows<InvalidOperationException>(() => NoesisGlyphCacheExpansion.ModActivation.IsEnabled(typeof(FakeManager), package));
        FakeManager.Instance = new FakeManager { Directories = new[] { new FakeDirectory { DirectoryInfo = null } } };
        RequireThrows<InvalidOperationException>(() => NoesisGlyphCacheExpansion.ModActivation.IsEnabled(typeof(FakeManager), package));
    }

    private static void Require(bool result, string message)
    {
        if (!result) throw new InvalidOperationException(message);
    }

    private static void RequireThrows<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }

    public sealed class FakeManager
    {
        public static FakeManager Instance { get; set; }
        public FakeDirectory[] Directories { get; set; }
    }

    public sealed class FakeDirectory
    {
        public DirectoryInfo DirectoryInfo { get; set; }
        public bool IsChecked { get; set; }
    }
}
