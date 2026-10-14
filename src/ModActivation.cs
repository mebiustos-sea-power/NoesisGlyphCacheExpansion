using System;
using System.Collections;
using System.IO;
using System.Reflection;

namespace NoesisGlyphCacheExpansion
{
    internal static class ModActivation
    {
        internal static bool IsEnabled(Type fileManagerType, string packagePath)
        {
            if (fileManagerType == null) throw new ArgumentNullException(nameof(fileManagerType));
            PropertyInfo instance = RequiredProperty(fileManagerType, "Instance",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            object manager = instance.GetValue(null)
                ?? throw new InvalidOperationException("Sea Power's file manager is not ready.");
            PropertyInfo directoriesProperty = RequiredProperty(fileManagerType, "Directories",
                BindingFlags.Instance | BindingFlags.Public);
            IEnumerable directories = directoriesProperty.GetValue(manager) as IEnumerable
                ?? throw new InvalidOperationException("Sea Power's mod directories are unavailable.");
            string normalized = Normalize(packagePath);
            foreach (object directory in directories)
            {
                if (directory == null) throw new InvalidOperationException("Sea Power registered an empty mod directory entry.");
                Type directoryType = directory.GetType();
                PropertyInfo infoProperty = RequiredProperty(directoryType, "DirectoryInfo",
                    BindingFlags.Instance | BindingFlags.Public);
                DirectoryInfo info = infoProperty.GetValue(directory) as DirectoryInfo
                    ?? throw new InvalidOperationException("A mod directory has no directory information.");
                if (!string.Equals(Normalize(info.FullName), normalized, StringComparison.OrdinalIgnoreCase))
                    continue;
                PropertyInfo checkedProperty = RequiredProperty(directoryType, "IsChecked",
                    BindingFlags.Instance | BindingFlags.Public);
                if (checkedProperty.PropertyType != typeof(bool))
                    throw new InvalidOperationException("Sea Power's IsChecked property is no longer a Boolean.");
                return (bool)checkedProperty.GetValue(directory);
            }
            throw new InvalidOperationException("The mod folder was not registered by Sea Power: " + packagePath);
        }

        private static PropertyInfo RequiredProperty(Type type, string name, BindingFlags flags)
        {
            return type.GetProperty(name, flags) ?? throw new MissingMemberException(type.FullName, name);
        }

        private static string Normalize(string path)
        {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}
