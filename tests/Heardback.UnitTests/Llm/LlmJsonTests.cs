using System.Text.Json;
using Heardback.Infrastructure.Llm.Structured;

namespace Heardback.UnitTests.Llm;

public sealed class LlmJsonTests
{
    [Fact]
    public void TryRead_ExactShape_ReadsAllFields()
    {
        var read = LlmJson.TryRead<TestAnswer>(
            """{"title":"Dev","seniority":"Junior","skills":["Go"],"location":"Istanbul"}""", out var answer);

        Assert.True(read);
        Assert.NotNull(answer);
        Assert.Equal("Dev", answer.Title);
        Assert.Equal(TestSeniority.Junior, answer.Seniority);
        Assert.Equal(["Go"], answer.Skills);
        Assert.Equal("Istanbul", answer.Location);
    }

    [Theory]
    [InlineData("""{"title":"Dev","seniority":"Junior","skills":[],"location":null,"salary":"high"}""")] // unknown field
    [InlineData("""{"title":"Dev","seniority":"Junior","skills":[]}""")] // missing field, even a nullable one
    [InlineData("""{"title":null,"seniority":"Junior","skills":[],"location":null}""")] // null for non-nullable
    [InlineData("""{"title":"Dev","seniority":"Junior","skills":null,"location":null}""")] // null list
    [InlineData("""{"title":42,"seniority":"Junior","skills":[],"location":null}""")] // wrong type
    [InlineData("""{"title":"Dev","seniority":1,"skills":[],"location":null}""")] // enum as number
    [InlineData("""{"title":"Dev","seniority":"Principal","skills":[],"location":null}""")] // undefined enum
    [InlineData("""{"Title":"Dev","seniority":"Junior","skills":[],"location":null}""")] // property in other case
    [InlineData("""{"title":"Dev","title":"Other","seniority":"Junior","skills":[],"location":null}""")] // duplicate
    [InlineData("""{"title":"Dev","seniority":"Junior","skills":[],"location":null,}""")] // trailing comma
    [InlineData("```json\n{\"title\":\"Dev\",\"seniority\":\"Junior\",\"skills\":[],\"location\":null}\n```")] // fences
    [InlineData("""{"title":"Dev","seniority":"Junior","skills":[],"location":null} and more""")] // trailing text
    [InlineData("null")]
    [InlineData("")]
    public void TryRead_AnyDeviation_IsRefused(string content)
    {
        Assert.False(LlmJson.TryRead<TestAnswer>(content, out var answer));
        Assert.Null(answer);
    }

    // Still a defined value, so accepting it saves a retry without letting anything unknown through.
    [Fact]
    public void TryRead_EnumNameInOtherCase_IsAccepted()
    {
        Assert.True(LlmJson.TryRead<TestAnswer>(
            """{"title":"Dev","seniority":"junior","skills":[],"location":null}""", out var answer));
        Assert.Equal(TestSeniority.Junior, answer!.Seniority);
    }

    [Fact]
    public void SchemaFor_AnswerType_MeetsGroqStrictModeRules()
    {
        var schema = LlmJson.SchemaFor(typeof(TestAnswer));

        Assert.Equal("object", schema.GetProperty("type").GetString());
        Assert.False(schema.GetProperty("additionalProperties").GetBoolean());
        var properties = schema.GetProperty("properties").EnumerateObject().Select(property => property.Name).ToList();
        var required = schema.GetProperty("required").EnumerateArray().Select(name => name.GetString()).ToList();
        Assert.Equal(["title", "seniority", "skills", "location"], properties);
        Assert.Equal(properties.Order(), required.Order());
    }

    [Fact]
    public void SchemaFor_EnumAndNullable_AreDescribedByNameAndAsUnion()
    {
        var properties = LlmJson.SchemaFor(typeof(TestAnswer)).GetProperty("properties");

        var seniority = properties.GetProperty("seniority").GetProperty("enum").EnumerateArray()
            .Select(value => value.GetString());
        Assert.Equal(["Junior", "Senior", "Unknown"], seniority);
        Assert.Equal("string", properties.GetProperty("title").GetProperty("type").GetString());
        var location = properties.GetProperty("location").GetProperty("type").EnumerateArray()
            .Select(type => type.GetString());
        Assert.Equal(["string", "null"], location);
        Assert.Equal("array", properties.GetProperty("skills").GetProperty("type").GetString());
    }

    [Fact]
    public void SchemaFor_SameType_ReturnsCachedSchema()
    {
        Assert.True(JsonElement.DeepEquals(LlmJson.SchemaFor(typeof(TestAnswer)), LlmJson.SchemaFor(typeof(TestAnswer))));
    }
}
