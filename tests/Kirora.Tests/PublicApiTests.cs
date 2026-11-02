using System.Reflection;

namespace Kirora.Tests;

public sealed class PublicApiTests
{
    [Fact]
    public void PhaseTwoDeltaExactlyMatchesDocumentedMigration()
    {
        var expected = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Fixtures/public-api-phase2.txt"));
        var actual = new HashSet<string>(StringComparer.Ordinal);
        foreach (var type in typeof(KiroraClient).Assembly.GetExportedTypes())
        {
            actual.Add($"T:{type.FullName}:{type.BaseType}:{type.IsSealed}:{type.IsAbstract}");
            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (member is MethodInfo method && (!method.IsSpecialName || method.Name.StartsWith("op_", StringComparison.Ordinal))) actual.Add($"M:{type.FullName}:{method}");
                else if (member is ConstructorInfo constructor) actual.Add($"C:{type.FullName}:{constructor}");
                else if (member is PropertyInfo property) actual.Add($"P:{type.FullName}:{property.PropertyType}:{property.Name}:get={property.GetMethod is not null}:set={property.SetMethod is not null}");
                else if (member is FieldInfo field) actual.Add($"F:{type.FullName}:{field.FieldType}:{field.Name}");
            }
        }
        MigrationApiAssertions.HistoricalDelta("public-api-phase2.txt", expected, actual);
    }
}

public sealed class XmlDocumentationTests
{
    [Fact]
    public void AllResourceClassesAndOperationsHaveXmlDocumentation()
    {
        var document = System.Xml.Linq.XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Kirora.xml"));
        var names = document.Descendants("member").Select(member => member.Attribute("name")!.Value).ToArray();
        foreach (var type in typeof(KiroraClient).Assembly.GetExportedTypes().Where(t => t.Namespace == "Kirora.Resources"))
        {
            Assert.Contains("T:" + type.FullName, names);
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                Assert.Contains(names, name => name.StartsWith("M:" + type.FullName + "." + method.Name + "(", StringComparison.Ordinal));
        }
    }
}

public sealed class PhaseThreeApiTests
{
    [Fact]
    public void PhaseThreeDeltaExactlyMatchesDocumentedMigration()
    {
        var current = typeof(KiroraClient).Assembly.GetExportedTypes().SelectMany(type =>
            new[] { $"T:{type.FullName}:{type.BaseType}:{type.IsSealed}:{type.IsAbstract}" }.Concat(
                type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Select(member => member switch
                {
                    MethodInfo method when !method.IsSpecialName || method.Name.StartsWith("op_", StringComparison.Ordinal) => $"M:{type.FullName}:{method}",
                    ConstructorInfo constructor => $"C:{type.FullName}:{constructor}",
                    PropertyInfo property => $"P:{type.FullName}:{property.PropertyType}:{property.Name}:get={property.GetMethod is not null}:set={property.SetMethod is not null}",
                    FieldInfo field => $"F:{type.FullName}:{field.FieldType}:{field.Name}",
                    _ => string.Empty
                }))).ToHashSet(StringComparer.Ordinal);
        MigrationApiAssertions.HistoricalDelta("public-api-phase3.txt", File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Fixtures/public-api-phase3.txt")), current);
        Assert.Equal(File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Fixtures/public-api-upstream-30043317.txt")).Order(StringComparer.Ordinal), current.Where(s => s.Length > 0).Order(StringComparer.Ordinal));
    }
}

internal static class MigrationApiAssertions
{
    internal static void HistoricalDelta(string baseline, IEnumerable<string> historical, IEnumerable<string> current)
    {
        using var report = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures/migration-api-changes.json")));
        var documented = report.RootElement.GetProperty("removedSignatures").GetProperty(baseline).EnumerateArray().Select(v => v.GetString()!).Order(StringComparer.Ordinal);
        Assert.Equal(documented, historical.Except(current).Order(StringComparer.Ordinal));
    }
}
