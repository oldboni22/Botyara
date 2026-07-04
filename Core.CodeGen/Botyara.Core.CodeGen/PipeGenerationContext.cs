using Microsoft.CodeAnalysis;

namespace Core.CodeGen;

public enum PipeInvocationType
{
    None,
    Void,
    ValueTask,
    Task,
}

public record struct PipeGenerationContext(
    string ClassName,
    PipeInvocationType InHandlerInvocationType,
    PipeInvocationType OutHandlerInvocationType,
    bool IsAmbiguous,
    bool NotImplementing,
    Location Location)
{
    public bool IsInit { get; set; } = false;
}
