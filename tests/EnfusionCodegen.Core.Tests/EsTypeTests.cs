using EnfusionCodegen.Core.Model;
using EnfusionCodegen.Core.OpenApi;
using Xunit;

namespace EnfusionCodegen.Core.Tests;

public class EsTypeTests
{
    [Fact]
    public void FromSchema_MapsNumberToFloat() =>
        Assert.Equal("float", EsType.FromSchema(new OpenApiSchema { Types = OpenApiSchemaType.Number }).TypeName);

    [Fact]
    public void FromSchema_MapsIntegerWithoutEnumToInt()
    {
        var type = EsType.FromSchema(new OpenApiSchema { Types = OpenApiSchemaType.Integer });
        Assert.Equal("int", type.TypeName);
        Assert.False(type.IsEnum);
    }

    [Fact]
    public void FromSchema_MapsIntegerWithEnumToEnumReference()
    {
        var type = EsType.FromSchema(new OpenApiSchema
        {
            Types = OpenApiSchemaType.Integer,
            IntegerEnumValues = [1],
            ReferenceName = "MessageTypeDto",
        });

        Assert.Equal("MessageTypeDto", type.TypeName);
        Assert.True(type.IsEnum);
        Assert.Null(type.BaseTypeName);
    }

    [Fact]
    public void FromSchema_MapsObjectToClassWithJsonApiStructBase() =>
        Assert.Equal("JsonApiStruct", EsType.FromSchema(new OpenApiSchema { Types = OpenApiSchemaType.Object }).BaseTypeName);

    [Fact]
    public void FromSchema_MapsArrayOfReferenceToRefArraySyntax()
    {
        var type = EsType.FromSchema(new OpenApiSchema
        {
            Types = OpenApiSchemaType.Array,
            Items = new OpenApiSchema { Types = OpenApiSchemaType.Object, ReferenceName = "MessageDto" },
        });

        Assert.Equal("ref array<ref MessageDto>", type.TypeName);
        Assert.True(type.IsArray);
        Assert.Equal("{}", type.DefaultValueLiteral);
    }

    [Fact]
    public void FromSchema_MapsArrayOfReferencedEnumToRefArrayWithoutRefOnItemType()
    {
        var type = EsType.FromSchema(new OpenApiSchema
        {
            Types = OpenApiSchemaType.Array,
            Items = new OpenApiSchema
            {
                Types = OpenApiSchemaType.Integer,
                IntegerEnumValues = [0, 1],
                ReferenceName = "MessageTypeDto",
            },
        });

        Assert.Equal("ref array<MessageTypeDto>", type.TypeName);
    }

    [Fact]
    public void FromSchema_MapsArrayOfPrimitiveToRefArraySyntax() =>
        Assert.Equal("ref array<string>", EsType.FromSchema(new OpenApiSchema
        {
            Types = OpenApiSchemaType.Array,
            Items = new OpenApiSchema { Types = OpenApiSchemaType.String },
        }).TypeName);

    [Fact]
    public void FromSchema_MapsPlainReferenceToItsId()
    {
        var type = EsType.FromSchema(new OpenApiSchema { Types = OpenApiSchemaType.Object, ReferenceName = "CharacterDto" });
        Assert.Equal("CharacterDto", type.TypeName);
        Assert.True(type.IsReference);
    }

    [Fact]
    public void FromSchema_SetsIsNullable_FromSchemaTypeArray()
    {
        var type = EsType.FromSchema(new OpenApiSchema { Types = OpenApiSchemaType.String | OpenApiSchemaType.Null });
        Assert.True(type.IsNullable);
    }
}
