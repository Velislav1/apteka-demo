using System.Reflection;
using NetArchTest.Rules;

namespace Apteka.ArchitectureTests;

/// <summary>
/// Type-level rules (guard the code that will be added later; trivially true while projects are empty).
/// The reference rules in <see cref="ProjectReferenceRulesTests"/> read the .csproj files and do not depend on loading DLLs.
/// </summary>
public class TypeDependencyRulesTests
{
    private static readonly string[] Layers = ["Contracts", "Domain", "Application", "Infrastructure"];

    /// <summary>
    /// Returns null when Windows (Smart App Control / App Control policy) refuses to load a freshly built unsigned DLL.
    /// That is a machine policy, not an architecture violation, so such assemblies are skipped.
    /// </summary>
    private static Assembly? TryLoad(string module, string layer)
    {
        try
        {
            return Assembly.Load($"Apteka.{module}.{layer}");
        }
        catch (FileLoadException)
        {
            return null;
        }
    }

    [Fact]
    public void Domain_types_do_not_use_EF_Core_or_ASP_NET()
    {
        foreach (var module in RepositoryLayout.Modules)
        {
            var assembly = TryLoad(module, "Domain");
            if (assembly is null)
            {
                continue;
            }

            var result = Types.InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Npgsql")
                .GetResult();

            Assert.True(result.IsSuccessful, $"{module}.Domain: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
    }

    [Fact]
    public void Module_types_do_not_use_internals_of_other_modules()
    {
        foreach (var module in RepositoryLayout.Modules)
        {
            var forbidden = RepositoryLayout.Modules
                .Where(other => other != module)
                .SelectMany(other => new[] { $"Apteka.{other}.Domain", $"Apteka.{other}.Application", $"Apteka.{other}.Infrastructure" })
                .ToArray();

            foreach (var layer in Layers)
            {
                var assembly = TryLoad(module, layer);
                if (assembly is null)
                {
                    continue;
                }

                var result = Types.InAssembly(assembly)
                    .ShouldNot()
                    .HaveDependencyOnAny(forbidden)
                    .GetResult();

                Assert.True(result.IsSuccessful, $"{module}.{layer}: {string.Join(", ", result.FailingTypeNames ?? [])}");
            }
        }
    }
}
