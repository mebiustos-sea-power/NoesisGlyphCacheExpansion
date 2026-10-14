using System;
using System.Reflection;
using HarmonyLib;

namespace NoesisGlyphCacheExpansion
{
    internal static class GlyphCacheInstaller
    {
        private static Action<string> logMessage;
        private static bool installed;
        private static bool reported;
        private static Harmony harmony;

        internal static void Install(string patchId, Action<string> log)
        {
            if (installed) return;
            if (log == null) throw new ArgumentNullException(nameof(log));
            MethodInfo getter = AccessTools.DeclaredMethod(typeof(NoesisSettings), "Get", Type.EmptyTypes);
            if (getter == null || !getter.IsStatic || getter.ReturnType != typeof(NoesisSettings))
                throw new MissingMethodException("NoesisSettings", "Get()");
            harmony = new Harmony(patchId);
            logMessage = log;
            try
            {
                // The renderer reads Get() before creating a view. Patch the getter
                // without loading settings or initializing native rendering at startup.
                harmony.Patch(getter, postfix: new HarmonyMethod(typeof(GlyphCacheInstaller), "SettingsPostfix"));
                installed = true;
                logMessage("Enabled: Noesis glyph cache target is 4096x4096. Awaiting settings load.");
            }
            catch
            {
                harmony.UnpatchSelf();
                throw;
            }
        }

        private static void SettingsPostfix(NoesisSettings __result)
        {
            NoesisSettings.TextureSize previous = CacheSizeOverride.Apply(__result);
            if (!reported || previous != CacheSizeOverride.Target)
            {
                logMessage("Noesis glyph cache setting: " + previous + " -> " + CacheSizeOverride.Target);
                reported = true;
            }
        }
    }
}
