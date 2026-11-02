using System.Reflection.Metadata;
using System.Reflection;
using System.Runtime.Loader;

if (args.Length is 3 or 4 && args[0] == "--source-link")
{
    using var stream = File.OpenRead(args[1]);
    using var provider = MetadataReaderProvider.FromPortablePdbStream(stream);
    var reader = provider.GetMetadataReader();
    foreach (var handle in reader.CustomDebugInformation)
    {
        var info = reader.GetCustomDebugInformation(handle);
        if (reader.GetGuid(info.Kind) == new Guid("CC110556-A091-4D38-9FEC-25AB9A351A6A"))
        {
            File.WriteAllBytes(args[2], reader.GetBlobBytes(info.Value));
            if (args.Length == 4)
                File.WriteAllText(args[3], System.Text.Json.JsonSerializer.Serialize(reader.Documents.Select(h => new { name = reader.GetString(reader.GetDocument(h).Name), embedded = reader.GetCustomDebugInformation(h).Any(c => reader.GetGuid(reader.GetCustomDebugInformation(c).Kind) == new Guid("0E8A571B-6926-466E-B4AD-8AB04611F5FE")) }).ToArray()));
            Console.WriteLine("Extracted SourceLink from the portable PDB.");
            return 0;
        }
    }
    Console.Error.WriteLine("Portable PDB has no SourceLink information.");
    return 1;
}

if (args.Length != 2) { Console.Error.WriteLine("Usage: <assembly path> <snapshot path>"); return 1; }
var context = new AssemblyLoadContext("ApiSnapshot", true);
context.Resolving += (_, name) => AssemblyLoadContext.Default.LoadFromAssemblyName(name);
var assembly = context.LoadFromAssemblyPath(Path.GetFullPath(args[0]));
var lines = new SortedSet<string>(StringComparer.Ordinal);
foreach (var type in assembly.GetExportedTypes())
{
    lines.Add($"T:{type.FullName}:{type.BaseType}:{type.IsSealed}:{type.IsAbstract}");
    foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
    {
        if (member is MethodInfo method && !method.IsSpecialName)
            lines.Add($"M:{type.FullName}:{method}");
        else if (member is MethodInfo op && op.Name.StartsWith("op_", StringComparison.Ordinal)) lines.Add($"M:{type.FullName}:{op}");
        else if (member is ConstructorInfo constructor) lines.Add($"C:{type.FullName}:{constructor}");
        else if (member is PropertyInfo property)
            lines.Add($"P:{type.FullName}:{property.PropertyType}:{property.Name}:get={property.GetMethod is not null}:set={property.SetMethod is not null}");
        else if (member is FieldInfo field) lines.Add($"F:{type.FullName}:{field.FieldType}:{field.Name}");
    }
}
File.WriteAllLines(args[1], lines);
Console.WriteLine($"Wrote {lines.Count} public API signatures.");
return 0;
