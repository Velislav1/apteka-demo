using System.Text.RegularExpressions;

namespace Apteka.ArchitectureTests;

/// <summary>
/// Rules checked against the .csproj files, so they also hold for projects that are still empty
/// (the compiler drops unused references from assembly metadata).
/// </summary>
public class ProjectReferenceRulesTests
{
    private const string SharedKernel = "Apteka.SharedKernel";

    [Fact]
    public void Module_projects_see_other_modules_only_through_Contracts()
    {
        var violations = new List<string>();

        foreach (var project in RepositoryLayout.SourceProjects)
        {
            var ownModule = RepositoryLayout.ModuleOf(project.Name);
            if (ownModule is null)
            {
                continue;
            }

            foreach (var reference in project.ProjectReferences)
            {
                var referencedModule = RepositoryLayout.ModuleOf(reference);
                if (referencedModule is not null && referencedModule != ownModule && !reference.EndsWith(".Contracts", StringComparison.Ordinal))
                {
                    violations.Add($"{project.Name} -> {reference}");
                }
            }
        }

        Assert.True(violations.Count == 0, "Only <Module>.Contracts may be referenced across modules:\n" + string.Join('\n', violations));
    }

    [Fact]
    public void Domain_depends_on_nothing_except_SharedKernel()
    {
        foreach (var module in RepositoryLayout.Modules)
        {
            var domain = RepositoryLayout.Layer(module, "Domain");

            Assert.True(
                domain.ProjectReferences.All(r => r == SharedKernel),
                $"{domain.Name} references: {string.Join(", ", domain.ProjectReferences)}");
        }
    }

    [Fact]
    public void Domain_has_no_framework_packages()
    {
        foreach (var module in RepositoryLayout.Modules)
        {
            var domain = RepositoryLayout.Layer(module, "Domain");

            Assert.True(
                domain.PackageReferences.All(p => !p.Contains("EntityFramework") && !p.Contains("Npgsql") && !p.Contains("AspNetCore")),
                $"{domain.Name} must not use EF Core, Npgsql or ASP.NET Core packages.");
        }
    }

    [Fact]
    public void Contracts_depend_on_nothing_except_SharedKernel()
    {
        foreach (var module in RepositoryLayout.Modules)
        {
            var contracts = RepositoryLayout.Layer(module, "Contracts");

            Assert.True(
                contracts.ProjectReferences.All(r => r == SharedKernel),
                $"{contracts.Name} references: {string.Join(", ", contracts.ProjectReferences)}");
        }
    }

    [Fact]
    public void Application_does_not_reference_Infrastructure()
    {
        foreach (var module in RepositoryLayout.Modules)
        {
            var application = RepositoryLayout.Layer(module, "Application");

            Assert.DoesNotContain(application.ProjectReferences, r => r.EndsWith(".Infrastructure", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void BuildingBlocks_do_not_reference_modules()
    {
        var blocks = RepositoryLayout.SourceProjects.Where(p => p.Name is SharedKernel or "Apteka.Infrastructure.Common");

        foreach (var block in blocks)
        {
            Assert.DoesNotContain(block.ProjectReferences, r => RepositoryLayout.ModuleOf(r) is not null);
        }

        var sharedKernel = RepositoryLayout.SourceProjects.Single(p => p.Name == SharedKernel);
        Assert.Empty(sharedKernel.ProjectReferences);
    }

    [Fact]
    public void Modules_are_registered_only_in_the_Host()
    {
        var hostFolder = Path.Combine(RepositoryLayout.Root, "src", "Host", "Apteka.Web") + Path.DirectorySeparatorChar;
        var call = new Regex($@"\.Add({string.Join('|', RepositoryLayout.Modules)})Module\(");

        var offenders = Directory.GetFiles(Path.Combine(RepositoryLayout.Root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.StartsWith(hostFolder, StringComparison.OrdinalIgnoreCase))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(f => call.IsMatch(File.ReadAllText(f)))
            .ToList();

        Assert.True(offenders.Count == 0, "Module registration outside the Host:\n" + string.Join('\n', offenders));
    }

    [Fact]
    public void Host_registers_every_module()
    {
        var program = File.ReadAllText(Path.Combine(RepositoryLayout.Root, "src", "Host", "Apteka.Web", "Program.cs"));

        foreach (var module in RepositoryLayout.Modules)
        {
            Assert.Contains($".Add{module}Module(", program);
        }
    }
}
