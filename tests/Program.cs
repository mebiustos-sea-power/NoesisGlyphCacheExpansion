using System;
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
            ActivationRespectsCheckboxAndFolder();
            ActivationReportsMissingState();
            Console.WriteLine("PASS: cache override, preserved settings, repeat application, checkbox states, path matching and activation errors.");
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
