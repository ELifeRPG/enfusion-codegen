using System.Collections.Generic;
using EnfusionCodegen.Core.Model;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Xunit;

namespace EnfusionCodegen.Core.Tests;

public class EsTypeTests
{
    [Fact]
    public void FromSchema_MapsNumberToFloat()
    {
        var type = EsType.FromSchema(new OpenApiSchema { Type = "number" });
        Assert.Equal("float", type.TypeName);
    }

    [Fact]
    public void FromSchema_MapsIntegerWithoutEnumToInt()
    {
        var type = EsType.FromSchema(new OpenApiSchema { Type = "integer" });
        Assert.Equal("int", type.TypeName);
        Assert.False(type.IsEnum);
    }

    [Fact]
    public void FromSchema_MapsIntegerWithEnumToEnumReference()
    {
        var schema = new OpenApiSchema
        {
            Type = "integer",
            Enum = new List<IOpenApiAny> { new OpenApiInteger(1) },
            Reference = new OpenApiReference { Id = "MessageTypeDto" },
        };

        var type = EsType.FromSchema(schema);

        Assert.Equal("MessageTypeDto", type.TypeName);
        Assert.True(type.IsEnum);
        Assert.Null(type.BaseTypeName);
    }

    [Fact]
    public void FromSchema_MapsObjectToClassWithJsonApiStructBase()
    {
        var type = EsType.FromSchema(new OpenApiSchema { Type = "object" });
        Assert.Equal("JsonApiStruct", type.BaseTypeName);
    }

    [Fact]
    public void FromSchema_MapsArrayOfReferenceToRefArraySyntax()
    {
        var schema = new OpenApiSchema
        {
            Type = "array",
            Items = new OpenApiSchema { Type = "object", Reference = new OpenApiReference { Id = "MessageDto" } },
        };

        var type = EsType.FromSchema(schema);

        Assert.Equal("ref array<ref MessageDto>", type.TypeName);
        Assert.True(type.IsArray);
        Assert.Equal("{}", type.DefaultValueLiteral);
    }

    [Fact]
    public void FromSchema_MapsArrayOfReferencedEnumToRefArrayWithoutRefOnItemType()
    {
        // Enforce Script enums are value types, so "ref array<ref SomeEnum>" is
        // invalid — only the array itself is a ref, not each enum element.
        var schema = new OpenApiSchema
        {
            Type = "array",
            Items = new OpenApiSchema
            {
                Type = "integer",
                Enum = new List<IOpenApiAny> { new OpenApiInteger(0), new OpenApiInteger(1) },
                Reference = new OpenApiReference { Id = "MessageTypeDto" },
            },
        };

        var type = EsType.FromSchema(schema);

        Assert.Equal("ref array<MessageTypeDto>", type.TypeName);
    }

    [Fact]
    public void FromSchema_MapsArrayOfPrimitiveToRefArraySyntax()
    {
        var schema = new OpenApiSchema
        {
            Type = "array",
            Items = new OpenApiSchema { Type = "string" },
        };

        var type = EsType.FromSchema(schema);

        Assert.Equal("ref array<string>", type.TypeName);
    }

    [Fact]
    public void FromSchema_MapsPlainReferenceToItsId()
    {
        var schema = new OpenApiSchema { Type = "object", Reference = new OpenApiReference { Id = "CharacterDto" } };

        var type = EsType.FromSchema(schema);

        Assert.Equal("CharacterDto", type.TypeName);
        Assert.True(type.IsReference);
    }

    [Fact]
    public void FromSchema_SetsIsNullable_FromSchemaNullableFlag()
    {
        var schema = new OpenApiSchema { Type = "string", Nullable = true };

        var type = EsType.FromSchema(schema);

        Assert.True(type.IsNullable);
    }
}
