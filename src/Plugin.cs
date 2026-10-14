using System;
using System.IO;
using System.Linq;
using AnchorChain;
using BepInEx.Logging;

namespace NoesisGlyphCacheExpansion
{
    [ACPlugin(ModId, "NoesisGUI Glyph Cache Expansion", "1.0.0")]
    public sealed class Plugin : IAnchorChainMod
    {
        public const string ModId = "mebiustos.seapower.noesisglyphcacheexpansion";
        private static readonly ManualLogSource Log = Logger.CreateLogSource("NoesisGUI Glyph Cache Expansion");

        public void TriggerEntryPoint()
        {
            try
            {
                string packagePath = Path.GetDirectoryName(typeof(Plugin).Assembly.Location);
                if (string.IsNullOrEmpty(packagePath))
                    throw new InvalidOperationException("The plugin's mod folder could not be determined.");
                var gameAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .SingleOrDefault(assembly => assembly.GetName().Name == "Seapower-Scripts");
                if (gameAssembly == null)
                    throw new InvalidOperationException("Sea Power's game assembly is not loaded.");
                Type fileManagerType = gameAssembly.GetType("SeaPower.FileManager", true);
                // AnchorChain loads DLLs even from unchecked mods. Respect the
                // game's saved checkbox once at startup; changes require a restart.
                if (!ModActivation.IsEnabled(fileManagerType, packagePath))
                {
                    Log.LogInfo("Disabled in the mod manager; no cache override installed.");
                    return;
                }
                GlyphCacheInstaller.Install(ModId, message => Log.LogInfo(message));
            }
            catch (Exception error)
            {
                Log.LogError("Initialization failed: " + error);
                throw;
            }
        }
    }
}
