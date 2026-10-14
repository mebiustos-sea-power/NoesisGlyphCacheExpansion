using System;

namespace NoesisGlyphCacheExpansion
{
    internal static class CacheSizeOverride
    {
        internal const NoesisSettings.TextureSize Target = NoesisSettings.TextureSize._4096x4096;

        internal static NoesisSettings.TextureSize Apply(NoesisSettings settings)
        {
            if (ReferenceEquals(settings, null))
                throw new InvalidOperationException("Noesis settings asset is missing; cannot set the glyph cache to 4096x4096.");
            NoesisSettings.TextureSize previous = settings.glyphTextureSize;
            // Change the runtime field only; no asset serialization or live renderer reset.
            settings.glyphTextureSize = Target;
            return previous;
        }
    }
}
