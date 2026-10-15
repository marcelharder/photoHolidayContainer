namespace photoHolidayContainer.data;

public sealed class LocationImageStorage
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".avif",
        ".bmp",
        ".gif",
        ".jpeg",
        ".jpg",
        ".png",
        ".tif",
        ".tiff",
        ".webp"
    };

    private readonly string _rootPath;

    public LocationImageStorage(string rootPath)
    {
        _rootPath = Path.GetFullPath(rootPath);
    }

    public bool TryGetImages(string relativeDirectory, out List<string> imagePaths)
    {
        imagePaths = [];

        Console.WriteLine($"Relative directory: '{relativeDirectory}'");

        if (!TryResolvePath(relativeDirectory, allowRoot: false, out var directoryPath)
            || !Directory.Exists(directoryPath)
            || ContainsReparsePoint(directoryPath))
        {
            return false;
        }

        var pendingDirectories = new Stack<string>();
        pendingDirectories.Push(directoryPath);

        while (pendingDirectories.TryPop(out var currentDirectory))
        {
            foreach (var entry in Directory.EnumerateFileSystemEntries(currentDirectory))
            {
                var attributes = File.GetAttributes(entry);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                {
                    continue;
                }

                if ((attributes & FileAttributes.Directory) != 0)
                {
                    pendingDirectories.Push(entry);
                }
                else if (SupportedExtensions.Contains(Path.GetExtension(entry)))
                {
                    imagePaths.Add(entry);
                }
            }
        }

        imagePaths.Sort(StringComparer.OrdinalIgnoreCase);
        return true;
    }

    public bool TryGetImage(string relativePath, out string imagePath)
    {
        imagePath = string.Empty;

        if (!TryResolvePath(relativePath, allowRoot: false, out var fullPath)
            || !SupportedExtensions.Contains(Path.GetExtension(fullPath))
            || !File.Exists(fullPath))
        {
            return false;
        }

        if (ContainsReparsePoint(fullPath))
            return false;

        imagePath = fullPath;
        return true;
    }

    public string GetImageUrl(string imagePath)
    {
        var relativePath = Path.GetRelativePath(_rootPath, imagePath);
        var escapedPath = string.Join(
            '/',
            relativePath.Split(Path.DirectorySeparatorChar)
                .Select(Uri.EscapeDataString));

        return $"/api/location-images/{escapedPath}";
    }
    private bool TryResolvePath(string relativePath, bool allowRoot, out string fullPath)
{
    fullPath = string.Empty;

    Console.WriteLine($"_rootPath: '{_rootPath}'");
    Console.WriteLine($"relativePath: '{relativePath}'");

    if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
    {
        Console.WriteLine("FAILED: path is empty or rooted.");
        return false;
    }

    try
    {
        var combined = Path.Combine(_rootPath, relativePath);
        Console.WriteLine($"Combined: '{combined}'");

        fullPath = Path.GetFullPath(combined);
        Console.WriteLine($"FullPath: '{fullPath}'");
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine($"ArgumentException: {ex.Message}");
        return false;
    }
    catch (NotSupportedException ex)
    {
        Console.WriteLine($"NotSupportedException: {ex.Message}");
        return false;
    }

    var relativeToRoot = Path.GetRelativePath(_rootPath, fullPath);

    Console.WriteLine($"relativeToRoot: '{relativeToRoot}'");

    var result =
        (allowRoot || relativeToRoot != ".")
        && relativeToRoot != ".."
        && !relativeToRoot.StartsWith(
            $"..{Path.DirectorySeparatorChar}",
            StringComparison.Ordinal)
        && !Path.IsPathRooted(relativeToRoot);

    Console.WriteLine($"TryResolvePath result: {result}");

    return result;
}

   
    private bool ContainsReparsePoint(string path)
    {
        var currentPath = _rootPath;
        foreach (var segment in Path.GetRelativePath(_rootPath, path)
                     .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            currentPath = Path.Combine(currentPath, segment);
            if (IsReparsePoint(currentPath))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsReparsePoint(string path) =>
        (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
}
