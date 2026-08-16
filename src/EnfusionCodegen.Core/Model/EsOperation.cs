namespace EnfusionCodegen.Core.Model;

public enum EsHttpVerb { Get, Post, Put, Delete }

public record EsOperation(
    string OperationName,
    string Tag,
    EsHttpVerb Verb,
    string PathTemplate,
    IReadOnlyList<EsParameter> Parameters,
    EsType? RequestBodyType,
    EsType? ResponseType);
