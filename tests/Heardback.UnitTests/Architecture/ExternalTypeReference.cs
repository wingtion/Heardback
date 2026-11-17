using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Heardback.UnitTests.Architecture;

/// <summary>
/// A type from another assembly that an assembly's metadata refers to. The compiler records one for every
/// external type used anywhere: signatures, method bodies, attributes, and generic arguments.
/// </summary>
internal sealed record ExternalTypeReference(string AssemblyName, string Namespace, string TypeName)
{
    public static IReadOnlyList<ExternalTypeReference> ReadAll(string assemblyPath)
    {
        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var metadata = peReader.GetMetadataReader();

        var references = new List<ExternalTypeReference>();
        foreach (var handle in metadata.TypeReferences)
        {
            var reference = metadata.GetTypeReference(handle);
            var outermost = GetOutermostType(metadata, reference);
            if (outermost.ResolutionScope.Kind != HandleKind.AssemblyReference)
            {
                continue;
            }

            var assembly = metadata.GetAssemblyReference((AssemblyReferenceHandle)outermost.ResolutionScope);
            references.Add(new ExternalTypeReference(
                metadata.GetString(assembly.Name),
                metadata.GetString(outermost.Namespace),
                metadata.GetString(reference.Name)));
        }

        return references;
    }

    public override string ToString() => $"{Namespace}.{TypeName} ({AssemblyName})";

    // Nested types are scoped to their declaring type; only the outermost one names the assembly and namespace.
    private static TypeReference GetOutermostType(MetadataReader metadata, TypeReference reference)
    {
        while (reference.ResolutionScope.Kind == HandleKind.TypeReference)
        {
            reference = metadata.GetTypeReference((TypeReferenceHandle)reference.ResolutionScope);
        }

        return reference;
    }
}
