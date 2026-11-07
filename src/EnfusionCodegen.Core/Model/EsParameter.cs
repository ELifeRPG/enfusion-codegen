namespace EnfusionCodegen.Core.Model;

public enum EsParameterLocation { Path, Query, Header }

/// <summary>
/// <paramref name="Name"/> is the Enforce Script identifier. <see cref="WireName"/> is the name on the
/// wire, which differs only for headers: a header like <c>Idempotency-Key</c> is no valid identifier,
/// so it becomes <c>idempotencyKey</c> in the signature.
/// </summary>
public record EsParameter(string Name, EsType Type, EsParameterLocation Location)
{
    public string WireName { get; init; } = Name;
}
