using System.Xml.Linq;

namespace Apteka.ArchitectureTests;

internal sealed record ProjectInfo(string Name, string Path, IReadOnlyList<string> ProjectReferences, IReadOnlyList<string> PackageReferences);

internal static class RepositoryLayout
{
    public static readonly string[] Modules = ["Catalog", "Purchasing", "Inventory", "Sales", "Identity", "Audit"];

    public static string Root { get; } = FindRoot();

    public static IReadOnlyList<ProjectInfo> SourceProjects { get; } = Load("src");

    public static IEnumerable<ProjectInfo> ModuleProjects(string module) =>
        SourceProjects.Where(p => p.Name.StartsWith($"Apteka.{module}.", StringComparison.Ordinal));

    public static ProjectInfo Layer(string module, string layer) =>
        SourceProjects.Single(p => p.Name == $"Apteka.{module}.{layer}");

    /// <summary>The module a project belongs to, or null for BuildingBlocks and Host.</summary>
    public static string? ModuleOf(string projectName) =>
        Modules.FirstOrDefault(m => projectName.StartsWith($"Apteka.{m}.", StringComparison.Ordinal));

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Apteka.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Apteka.sln not found above the test output folder.");
    }

    private static List<ProjectInfo> Load(string folder) =>
        Directory.GetFiles(Path.Combine(Root, folder), "*.csproj", SearchOption.AllDirectories)
            .Select(path =>
            {
                var doc = XDocument.Load(path);
                var projectRefs = doc.Descendants("ProjectReference")
                    .Select(e => Path.GetFileNameWithoutExtension(((string)e.Attribute("Include")!).Replace('\\', '/')))
                    .ToList();
                var packageRefs = doc.Descendants("PackageReference")
                    .Select(e => (string)e.Attribute("Include")!)
                    .ToList();
                return new ProjectInfo(Path.GetFileNameWithoutExtension(path), path, projectRefs, packageRefs);
            })
            .ToList();
}
