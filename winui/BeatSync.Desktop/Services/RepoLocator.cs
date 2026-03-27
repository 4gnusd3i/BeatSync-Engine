using System.IO;

namespace BeatSync.Desktop.Services;

public static class RepoLocator
{
    private static readonly string[] RequiredMarkers =
    [
        Path.Combine("bin", "python-3.13.9-embed-amd64", "python.exe"),
        "beatsync_bridge.py",
    ];

    public static string LocateRepoRoot()
    {
        foreach (var start in GetCandidateRoots())
        {
            foreach (var candidate in EnumerateParents(start))
            {
                if (ContainsMarkers(candidate))
                {
                    return candidate;
                }
            }
        }

        throw new DirectoryNotFoundException(
            "BeatSync Desktop could not locate the repo root. The WinUI app expects to run inside the portable BeatSync folder structure."
        );
    }

    private static IEnumerable<string> GetCandidateRoots()
    {
        yield return AppContext.BaseDirectory;
        yield return Directory.GetCurrentDirectory();
    }

    private static IEnumerable<string> EnumerateParents(string startDirectory)
    {
        var current = new DirectoryInfo(Path.GetFullPath(startDirectory));
        while (current is not null)
        {
            yield return current.FullName;
            current = current.Parent;
        }
    }

    private static bool ContainsMarkers(string candidateRoot)
    {
        return RequiredMarkers.All(marker => File.Exists(Path.Combine(candidateRoot, marker)));
    }
}
