using NeoModLoader.api;
using NeoModLoader.constants;
using NeoModLoader.utils;

namespace NeoModLoader.services;

internal static class ExternalModHotLoadService
{
    private const int MaxScanDepth = 3;

    internal sealed class ExternalModCandidate
    {
        public ModDeclare Declaration { get; set; }
        public string SourceFolderPath { get; set; }
        public bool IsInstalled { get; set; }
        public bool IsLoaded { get; set; }
        public string Status { get; set; }
        public string InstalledPath { get; set; }
        public bool CanImportAndLoad => !IsInstalled && !IsLoaded && Declaration != null;
    }

    public static List<ExternalModCandidate> Scan(string rootPath)
    {
        var result = new List<ExternalModCandidate>();
        string normalizedRoot = NormalizePath(rootPath);
        if (string.IsNullOrWhiteSpace(normalizedRoot) || !Directory.Exists(normalizedRoot))
        {
            return result;
        }

        var seenFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string folder in EnumerateCandidateFolders(normalizedRoot))
        {
            string normalizedFolder = NormalizePath(folder);
            if (normalizedFolder == null || !seenFolders.Add(normalizedFolder))
            {
                continue;
            }

            ModDeclare declaration = null;
            try
            {
                declaration = ModInfoUtils.recogMod(normalizedFolder, false);
            }
            catch (Exception e)
            {
                LogService.LogWarning($"Failed to recognize external mod at {normalizedFolder}: {e.Message}");
            }

            if (declaration == null)
            {
                continue;
            }

            bool isLoaded = WorldBoxMod.LoadedMods.Any(mod => mod.GetDeclaration().UID == declaration.UID);
            bool isInstalled = ModInfoUtils.TryFindMod(declaration.UID, out ModDeclare installedMod);

            result.Add(new ExternalModCandidate
            {
                Declaration = declaration,
                SourceFolderPath = normalizedFolder,
                IsInstalled = isInstalled,
                IsLoaded = isLoaded,
                InstalledPath = installedMod?.FolderPath,
                Status = isLoaded ? "Loaded" : isInstalled ? "Installed" : "Ready"
            });
        }

        result.Sort((left, right) =>
        {
            int statusCompare = string.Compare(left.Status, right.Status, StringComparison.OrdinalIgnoreCase);
            if (statusCompare != 0)
            {
                return statusCompare;
            }

            return string.Compare(left.Declaration?.GetDisplayName(), right.Declaration?.GetDisplayName(),
                StringComparison.OrdinalIgnoreCase);
        });

        return result;
    }

    public static bool TryImportAndLoad(ExternalModCandidate candidate, out string message)
    {
        if (candidate?.Declaration == null)
        {
            message = "No mod selected.";
            return false;
        }

        if (candidate.IsLoaded)
        {
            message = $"{candidate.Declaration.Name} is already loaded.";
            return false;
        }

        if (candidate.IsInstalled)
        {
            message = $"{candidate.Declaration.Name} is already installed.";
            return false;
        }

        string sourceFolder = NormalizePath(candidate.SourceFolderPath);
        if (string.IsNullOrWhiteSpace(sourceFolder) || !Directory.Exists(sourceFolder))
        {
            message = "The selected source folder no longer exists.";
            return false;
        }

        string targetFolder = sourceFolder;
        bool copiedToModsFolder = false;
        if (!IsUnderDirectory(sourceFolder, Paths.ModsPath) && !IsUnderDirectory(sourceFolder, Paths.NativeModsPath))
        {
            string targetFolderName = Path.GetFileName(sourceFolder);
            if (string.IsNullOrWhiteSpace(targetFolderName))
            {
                targetFolderName = candidate.Declaration.UID;
            }

            targetFolder = NormalizePath(Path.Combine(Paths.ModsPath, targetFolderName));
            try
            {
                SystemUtils.CopyDirectory(sourceFolder, targetFolder);
                copiedToModsFolder = true;
            }
            catch (Exception e)
            {
                message = $"Failed to copy mod into {Paths.ModsPath}: {e.Message}";
                return false;
            }
        }

        ModDeclare copiedDeclare = ModInfoUtils.recogMod(targetFolder, true);
        if (copiedDeclare == null)
        {
            message = "The copied mod could not be recognized by NeoModLoader.";
            return false;
        }

        if (ModInfoUtils.TryGetRecognizedMod(copiedDeclare.UID, out ModDeclare recognizedMod) &&
            !PathsEqual(recognizedMod.FolderPath, copiedDeclare.FolderPath))
        {
            if (WorldBoxMod.TryGetLoadedMod(recognizedMod, out _))
            {
                message = $"{copiedDeclare.Name} is already loaded from another folder.";
                return false;
            }

            ModState existingState = WorldBoxMod.AllRecognizedMods.TryGetValue(recognizedMod, out ModState state)
                ? state
                : ModState.FAILED;
            WorldBoxMod.AllRecognizedMods.Remove(recognizedMod);
            WorldBoxMod.AllRecognizedMods[copiedDeclare] = existingState;
        }

        bool loaded = ModCompileLoadService.TryCompileAndLoadModAtRuntime(copiedDeclare);
        if (!loaded)
        {
            string failReason = copiedDeclare.FailReason.ToString().Trim();
            message = string.IsNullOrWhiteSpace(failReason)
                ? $"Failed to load {copiedDeclare.Name}."
                : $"Failed to load {copiedDeclare.Name}: {failReason}";
            return false;
        }

        message = copiedToModsFolder
            ? $"Loaded {copiedDeclare.Name} and saved it into the Mods folder."
            : $"Loaded {copiedDeclare.Name}.";
        return true;
    }

    public static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(path.Trim())
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<string> EnumerateCandidateFolders(string rootPath)
    {
        var queue = new Queue<(string Path, int Depth)>();
        queue.Enqueue((rootPath, 0));

        while (queue.Count > 0)
        {
            (string currentPath, int depth) = queue.Dequeue();

            if (!Directory.Exists(currentPath))
            {
                continue;
            }

            if (File.Exists(Path.Combine(currentPath, Paths.ModDeclarationFileName)))
            {
                yield return currentPath;
            }

            if (depth >= MaxScanDepth)
            {
                continue;
            }

            string[] subDirectories;
            try
            {
                subDirectories = Directory.GetDirectories(currentPath);
            }
            catch
            {
                continue;
            }

            foreach (string subDirectory in subDirectories)
            {
                string directoryName = Path.GetFileName(subDirectory);
                if (string.IsNullOrWhiteSpace(directoryName))
                {
                    continue;
                }

                if (directoryName.StartsWith(".") || Paths.IgnoreSearchDirectories.Contains(directoryName))
                {
                    continue;
                }

                queue.Enqueue((subDirectory, depth + 1));
            }
        }
    }

    private static bool IsUnderDirectory(string path, string directory)
    {
        string normalizedPath = NormalizePath(path);
        string normalizedDirectory = NormalizePath(directory);
        if (normalizedPath == null || normalizedDirectory == null)
        {
            return false;
        }

        string directoryWithSeparator = normalizedDirectory + Path.DirectorySeparatorChar;
        return normalizedPath.StartsWith(directoryWithSeparator, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalizedPath, normalizedDirectory, StringComparison.OrdinalIgnoreCase);
    }

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(NormalizePath(left), NormalizePath(right), StringComparison.OrdinalIgnoreCase);
    }
}
