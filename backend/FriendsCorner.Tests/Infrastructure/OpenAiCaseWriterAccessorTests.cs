using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FriendsCorner.Core.Accessors;
using FriendsCorner.Infrastructure.Accessors;

namespace FriendsCorner.Tests;

public class OpenAiCaseWriterAccessorTests
{
    private static readonly CaseRequest Request = new("a lighthouse in a storm", MysteryLevel.Easy);

    [Theory]
    [InlineData(MysteryLevel.Easy, "Difficulty: easy")]
    [InlineData(MysteryLevel.Hard, "Difficulty: hard")]
    public async Task The_input_carries_the_level(MysteryLevel level, string line)
    {
        var openAi = new StubOpenAi(Reply(CaseText(CaseFixture.Fair())));

        await Writer(openAi).Write(Request with { Level = level }, CancellationToken.None);

        Assert.Contains(line, (string?)Assert.Single(openAi.Sent).Body["input"]);
    }

    [Fact]
    public async Task The_schema_asks_for_minutes_and_a_mood()
    {
        var openAi = new StubOpenAi(Reply(CaseText(CaseFixture.Fair())));

        await Writer(openAi).Write(Request, CancellationToken.None);

        var schema = Assert.Single(openAi.Sent).Body["text"]!["format"]!["schema"]!;
        var minutes = schema["properties"]!["minutes"]!;
        Assert.Equal("integer", (string?)minutes["type"]);
        Assert.Equal(5, (int)minutes["minimum"]!);
        Assert.Equal(20, (int)minutes["maximum"]!);
        Assert.Equal(
            ["frost", "storm", "velvet", "garden", "smoke", "gilded"],
            schema["properties"]!["mood"]!["enum"]!.AsArray().Select(mood => (string)mood!));
    }

    [Fact]
    public async Task The_minutes_and_mood_come_back_on_the_case()
    {
        var written = CaseFixture.Fair() with { Minutes = 9, Mood = CaseMood.Frost };

        var read = await Writer(new StubOpenAi(Reply(CaseText(written)))).Write(Request, CancellationToken.None);

        Assert.Equal((9, CaseMood.Frost), (read!.Minutes, read.Mood));
    }

    [Fact]
    public async Task The_request_carries_the_key_the_model_the_theme_and_the_strict_schema()
    {
        var openAi = new StubOpenAi(Reply(CaseText(CaseFixture.Fair())));

        await Writer(openAi).Write(Request, CancellationToken.None);

        var sent = Assert.Single(openAi.Sent);
        Assert.Equal(HttpMethod.Post, sent.Method);
        Assert.Equal("https://api.openai.com/v1/responses", sent.Uri);
        Assert.Equal("Bearer test-key", sent.Authorization);
        Assert.Equal("test-model", (string?)sent.Body["model"]);
        Assert.Equal("low", (string?)sent.Body["reasoning"]!["effort"]);
        Assert.Contains("a lighthouse in a storm", sent.Body["input"]!.ToJsonString());
        Assert.False(string.IsNullOrWhiteSpace((string?)sent.Body["instructions"]));

        var format = sent.Body["text"]!["format"]!;
        Assert.Equal("json_schema", (string?)format["type"]);
        Assert.True((bool)format["strict"]!);
        // The model writes keys in this order: it settles the killer before it writes the leads.
        Assert.Equal(
            ["title", "setting", "mood", "victim", "suspects", "places", "solution", "leads", "proofs", "minutes"],
            format["schema"]!["required"]!.AsArray().Select(name => (string)name!));
    }

    [Fact]
    public async Task A_good_reply_becomes_the_case()
    {
        var written = await Writer(new StubOpenAi(Reply(CaseText(CaseFixture.Fair())))).Write(Request, CancellationToken.None);

        Assert.Equivalent(CaseFixture.Fair(), written, strict: true);
    }

    [Fact]
    public async Task A_server_error_is_no_case()
    {
        var openAi = new StubOpenAi(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        Assert.Null(await Writer(openAi).Write(Request, CancellationToken.None));
    }

    [Fact]
    public async Task A_refusal_is_no_case()
    {
        var refusal = new JsonObject
        {
            ["status"] = "completed",
            ["output"] = new JsonArray(Message(new JsonObject { ["type"] = "refusal", ["refusal"] = "I can't help with that." })),
        };

        Assert.Null(await Writer(new StubOpenAi(Json(refusal))).Write(Request, CancellationToken.None));
    }

    [Theory]
    [InlineData("""{"title":"Half a case"}""")]
    [InlineData("not json at all")]
    public async Task A_reply_that_breaks_the_schema_is_no_case(string text)
    {
        Assert.Null(await Writer(new StubOpenAi(Reply(text))).Write(Request, CancellationToken.None));
    }

    [Fact]
    public async Task With_no_key_it_is_no_case_without_calling_out()
    {
        var openAi = new StubOpenAi(Reply(CaseText(CaseFixture.Fair())));

        var written = await new OpenAiCaseWriterAccessor(new HttpClient(openAi), new CaseWriterSettings(ApiKey: null))
            .Write(Request, CancellationToken.None);

        Assert.Null(written);
        Assert.Empty(openAi.Sent);
    }

    [Fact]
    public async Task Being_cancelled_is_not_swallowed()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Writer(new StubOpenAi(Reply(CaseText(CaseFixture.Fair())))).Write(Request, cancelled.Token));
    }

    private static OpenAiCaseWriterAccessor Writer(StubOpenAi openAi) =>
        new(new HttpClient(openAi), new CaseWriterSettings("test-key", "test-model", "low"));

    // The wire shape the schema promises: camelCase names, lower-case kinds.
    private static string CaseText(Case @case) => JsonSerializer.Serialize(@case, new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    });

    private static HttpResponseMessage Reply(string text) => Json(new JsonObject
    {
        ["status"] = "completed",
        ["output"] = new JsonArray(
            new JsonObject { ["type"] = "reasoning", ["summary"] = new JsonArray() },
            Message(new JsonObject { ["type"] = "output_text", ["text"] = text })),
    });

    private static JsonObject Message(JsonObject content) => new()
    {
        ["type"] = "message",
        ["role"] = "assistant",
        ["content"] = new JsonArray(content),
    };

    private static HttpResponseMessage Json(JsonNode body) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
    };

    private sealed record SentRequest(HttpMethod Method, string Uri, string? Authorization, JsonNode Body);

    private sealed class StubOpenAi(HttpResponseMessage answer) : HttpMessageHandler
    {
        public List<SentRequest> Sent { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var body = JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken))!;
            Sent.Add(new SentRequest(request.Method, request.RequestUri!.ToString(), request.Headers.Authorization?.ToString(), body));
            return answer;
        }
    }
}
