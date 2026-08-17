namespace EnfusionCodegen.Core.Model;

public record EsClass(string Name, string BaseTypeName, IReadOnlyList<EsProperty> Properties);
