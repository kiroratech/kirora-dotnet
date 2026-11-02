using System.Reflection;
using System.Runtime.Loader;

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
