using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator

namespace Core.CodeGen;

[Generator]
public sealed class PipelineGenerator : IIncrementalGenerator 
{
    private const string UtilityName = "PipeUtility";
    
    private const string PipeName = "BotyaraPipe";
    
    private const string ContextName = "PipeContext";
    
    private const string PipeFullyQualifiedName = "Botyara.Core.Pipeline.BotyaraPipe";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.CompilationProvider.Select(static (compilation, ct) => 
            GetAllPipeContexts(compilation, ct));

        context.RegisterSourceOutput(provider, static (ctx, genContexts) =>
        {
            if (genContexts.IsEmpty) return;

            foreach (var genContext in genContexts)
            {
                if (!genContext.IsInit) continue;
                
                if (genContext.IsAmbiguous)
                {
                    ctx.ReportDiagnostic(PipeDiagnostics.Create.Ambiguous(genContext));
                }
                else if (genContext.NotImplementing)
                {
                    ctx.ReportDiagnostic(PipeDiagnostics.Create.IsNotImplementing(genContext));
                }
            }

            var validContexts = genContexts
                .Where(ctx => ctx is { IsInit: true, IsAmbiguous: false, NotImplementing: false })
                .ToImmutableArray();

            if (validContexts.IsEmpty) return;
            
            ctx.AddSource($"{UtilityName}.g.cs", GenerateMethod(validContexts));
        });
    }

    private static ImmutableArray<PipeGenerationContext> GetAllPipeContexts(Compilation compilation, CancellationToken ct)
    {
        var builder = ImmutableArray.CreateBuilder<PipeGenerationContext>();
        
        //Base BotyaraPipeClass
        var pipeBaseSymbol = compilation.GetTypeByMetadataName(PipeFullyQualifiedName);
        if (pipeBaseSymbol == null) return ImmutableArray<PipeGenerationContext>.Empty;

        //Scan the project's own asm 
        ScanNamespace(compilation.Assembly.GlobalNamespace, pipeBaseSymbol, builder, ct);
        
        foreach (var referencedAssembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            //Skip all non-Botyara asms
            if (!referencedAssembly.Name.StartsWith("Botyara"))
            {
                continue;
            }

            ScanNamespace(referencedAssembly.GlobalNamespace, pipeBaseSymbol, builder, ct);
        }

        return builder.ToImmutable();
    }

    private static void ScanNamespace(
        INamespaceSymbol @namespace,
        INamedTypeSymbol pipeBaseSymbol,
        ImmutableArray<PipeGenerationContext>.Builder builder, 
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        foreach (var type in @namespace.GetTypeMembers())
        {
            ScanType(type, pipeBaseSymbol, builder, ct);
        }

        foreach (var nestedNamespace in @namespace.GetNamespaceMembers())
        {
            ScanNamespace(nestedNamespace, pipeBaseSymbol, builder, ct);
        }
    }

    private static void ScanType(
        INamedTypeSymbol type, 
        INamedTypeSymbol pipeBaseSymbol,
        ImmutableArray<PipeGenerationContext>.Builder builder, 
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (!type.IsAbstract && IsSubclassOf(type, pipeBaseSymbol))
        {
            var context = AnalyzePipeSymbol(type, pipeBaseSymbol);
            if (context.IsInit)
            {
                builder.Add(context);
            }
        }

        foreach (var nestedType in type.GetTypeMembers())
        {
            ScanType(nestedType, pipeBaseSymbol, builder, ct);
        }
    }

    private static bool IsSubclassOf(INamedTypeSymbol type, INamedTypeSymbol pipeBaseSymbol)
    {
        //Iterate through the whole inheritance tree
        var current = type.BaseType;
        while (current != null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, pipeBaseSymbol))
                return true;
            current = current.BaseType;
        }
        return false;
    }

    private static PipeGenerationContext AnalyzePipeSymbol(INamedTypeSymbol classSymbol, INamedTypeSymbol pipeBaseSymbol)
    {
        var inInvocationType = PipeInvocationType.None;
        var outInvocationType = PipeInvocationType.None;

        //Map of i/o method overrides of the WHOLE inheritance tree
        var overriden = new HashSet<string>();
        var current = classSymbol;
        
        while (current is not null && !SymbolEqualityComparer.Default.Equals(current, pipeBaseSymbol))
        {
            var methods = current.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(method => method.IsOverride)
                .Select(method => method.Name);

            foreach (var method in methods)
            {
                overriden.Add(method);
            }

            current = current.BaseType;
        }

        var inCount = 0;
        var outCount = 0;
        
        if (overriden.Contains("InTask")) { inInvocationType = PipeInvocationType.Task; inCount++; }
        if (overriden.Contains("InValueTask")) { inInvocationType = PipeInvocationType.ValueTask; inCount++; }
        if (overriden.Contains("InVoid")) { inInvocationType = PipeInvocationType.Void; inCount++; }
        
        if (overriden.Contains("OutTask")) { outInvocationType = PipeInvocationType.Task; outCount++; }
        if (overriden.Contains("OutValueTask")) { outInvocationType = PipeInvocationType.ValueTask; outCount++; }
        if (overriden.Contains("OutVoid")) { outInvocationType = PipeInvocationType.Void; outCount++; }

        var isAmbiguous = inCount > 1 || outCount > 1;
        var notImplementing = inCount == 0 && outCount == 0;

        var location = Location.None;
        if ((isAmbiguous ^ notImplementing) && !classSymbol.DeclaringSyntaxReferences.IsEmpty)
        {
            location = classSymbol.DeclaringSyntaxReferences[0].GetSyntax().GetLocation();
        }

        return new PipeGenerationContext()
        {
            ClassName = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            IsAmbiguous = isAmbiguous,
            NotImplementing = notImplementing,
            Location = location,
            InHandlerInvocationType = inInvocationType,
            OutHandlerInvocationType = outInvocationType,
            IsInit = true
        };
    }

    private static string GenerateMethod(ImmutableArray<PipeGenerationContext> context)
    {
        static string GenerateIn(in PipeGenerationContext context)
        {
            return context.InHandlerInvocationType switch
            {
                PipeInvocationType.Void => "success = casted.InVoid(context)",
                _ => "success = await casted.InValueTask(context)",
            };
        }

        static string GenerateOut(in PipeGenerationContext context)
        {
            return context.OutHandlerInvocationType switch
            {
                PipeInvocationType.Void => "casted.OutVoid(context)",
                _ => "await casted.OutValueTask(context)",
            };
        }

        var sb = new StringBuilder("//<auto-generated />");

        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using Botyara.Core.Context;");
        sb.AppendLine("using Botyara.Core.Pipeline;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine();
        sb.AppendLine("namespace Botyara.Core.Utilities");
        sb.AppendLine("{");
        sb.AppendLine($"    public static class {UtilityName}");
        sb.AppendLine("    {");
        sb.AppendLine(
            $"        public static async Task HandleAsync({PipeName}[] members, {ContextName} context)");
        sb.AppendLine("        {");
        sb.AppendLine("             var i = 0;");
        sb.AppendLine("             var isShortCircuited = false;");
        sb.AppendLine("             for (; i < members.Length; i++)");
        sb.AppendLine("             {");
        sb.AppendLine("                 var success = true;");
        sb.AppendLine("                 switch (members[i])");
        sb.AppendLine("                 {");

        foreach (var step in context)
        {
            if(step.InHandlerInvocationType == PipeInvocationType.None) continue;
            
            sb.AppendLine($"                     case {step.ClassName} casted:");
            sb.AppendLine($"                         {GenerateIn(step)};");
            sb.AppendLine($"                         break;");
        }

        sb.AppendLine("                     case null: throw new ArgumentNullException(nameof(members));");
        sb.AppendLine("                 }");
        sb.AppendLine();
        sb.AppendLine("                 if (!success)");
        sb.AppendLine("                 {");
        sb.AppendLine("                      isShortCircuited = true;");
        sb.AppendLine("                      break;");
        sb.AppendLine("                 }");
        sb.AppendLine("             }");
        sb.AppendLine("");
        sb.AppendLine("             if (!isShortCircuited) i -= 1;");
        sb.AppendLine();

        sb.AppendLine("             for (; i >= 0; i--)");
        sb.AppendLine("             {");
        sb.AppendLine("                 switch (members[i])");
        sb.AppendLine("                 {");

        foreach (var step in context)
        {
            if(step.OutHandlerInvocationType == PipeInvocationType.None) continue;
            
            sb.AppendLine($"                     case {step.ClassName} casted:");
            sb.AppendLine($"                         {GenerateOut(step)};");
            sb.AppendLine($"                         break;");
        }

        sb.AppendLine("                 }");
        sb.AppendLine("             }");

        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
}
