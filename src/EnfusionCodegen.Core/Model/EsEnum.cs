namespace EnfusionCodegen.Core.Model;

public record EsEnum(string Name, IReadOnlyList<EsEnumMember> Members);
