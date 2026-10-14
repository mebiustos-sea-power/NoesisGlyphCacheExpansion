using System;

namespace NoesisGlyphCacheExpansion
{
    internal sealed class FailureBoundary
    {
        private readonly Action<string> logError;
        private bool stopped;

        internal FailureBoundary(Action<string> logError)
        {
            this.logError = logError ?? throw new ArgumentNullException(nameof(logError));
        }

        internal void Run(string operation, Action action)
        {
            if (stopped) return;
            try
            {
                action();
            }
            catch (Exception error)
            {
                // Stop before reporting so later callbacks cannot repeat the failure.
                stopped = true;
                logError(operation + " failed; glyph cache expansion disabled until restart. " + error);
            }
        }
    }
}
