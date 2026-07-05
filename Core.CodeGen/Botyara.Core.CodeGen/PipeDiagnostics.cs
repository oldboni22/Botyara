using Microsoft.CodeAnalysis;

namespace Core.CodeGen;

public static class PipeDiagnostics
{
    private static DiagnosticDescriptor AmbiguousDescriptor => new(
        id: "BOTYARA_ERR_001",
        title: "Ambiguous in/out",
        messageFormat: "Any handling member can implement in/out/finally only once.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        category: "Design");
    
    private static DiagnosticDescriptor IsNotImplementingDescriptor => new(
        id: "BOTYARA_WRN_001",
        title: "Is not implementing",
        messageFormat: "This handling member does not implement in/out/finally.",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        category: "Design");
    
    public static class Create
    {
        public static Diagnostic Ambiguous(in PipeGenerationContext context)
        {
            return Diagnostic.Create(AmbiguousDescriptor, context.Location);
        }

        public static Diagnostic IsNotImplementing(in PipeGenerationContext context)
        {
            return Diagnostic.Create(IsNotImplementingDescriptor, context.Location);
        }
    }
}
