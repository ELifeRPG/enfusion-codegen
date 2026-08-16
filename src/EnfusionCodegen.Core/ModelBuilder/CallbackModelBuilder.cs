using EnfusionCodegen.Core.Model;

namespace EnfusionCodegen.Core.ModelBuilder;

public static class CallbackModelBuilder
{
    public static IReadOnlyList<EsCallbackClass> Build(IReadOnlyList<EsOperation> operations, string prefix)
    {
        return operations
            .Where(o => o.ResponseType is not null && o.ResponseType.IsReference)
            .Select(o => o.ResponseType!.TypeName)
            .Distinct()
            .Select(responseModelName => new EsCallbackClass($"{prefix}{responseModelName}Callback", responseModelName))
            .ToList();
    }
}
