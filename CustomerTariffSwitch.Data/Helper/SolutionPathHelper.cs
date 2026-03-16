namespace CustomerTariffSwitch.Data.Helper;

internal static class SolutionPathHelper
{
    // Walks up from AppContext.BaseDirectory until it finds a directory that
    // contains the given markerFolderName, then returns that parent directory
    internal static string FindRootByMarker(string markerFolderName)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, markerFolderName)))
                return current.FullName;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not find a directory containing '{markerFolderName}' folder by walking up from '{AppContext.BaseDirectory}'.");
    }
}
