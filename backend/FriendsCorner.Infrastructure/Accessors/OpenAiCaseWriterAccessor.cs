using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FriendsCorner.Core.Accessors;
using FriendsCorner.Core.Engines.Mystery;

namespace FriendsCorner.Infrastructure.Accessors;

// Low effort writes a fair case in about 25 seconds; medium took 57 to 73,
// too close to the room's 60-second limit.
public sealed record CaseWriterSettings(
    string? ApiKey, string Model = CaseWriterSettings.DefaultModel, string Effort = CaseWriterSettings.DefaultEffort)
{
    public const string DefaultModel = "gpt-5.5-2026-04-23";
    public const string DefaultEffort = "low";
}

// One call to OpenAI's Responses API with a strict schema. Anything that is
// not a well-formed case comes back as null; only cancellation escapes, so
// the room's time limit still cuts the call off.
public sealed class OpenAiCaseWriterAccessor(HttpClient http, CaseWriterSettings settings) : ICaseWriterAccessor
{
    private static readonly Uri Endpoint = new("https://api.openai.com/v1/responses");
    private static readonly string Prompt = Resource("mystery-prompt.md");
    private static readonly string Schema = Resource("mystery-case.schema.json");

    // Missing or null fields fail to parse instead of reaching Core as nulls.
    private static readonly JsonSerializerOptions Wire = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
    };

    public async Task<Case?> Write(CaseRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            return null;
        }

        try
        {
            using var call = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = JsonContent.Create(Body(request)) };
            call.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

            using var response = await http.SendAsync(call, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var reply = await response.Content.ReadFromJsonAsync<JsonNode>(cancellationToken);
            return OutputText(reply) is { } text ? JsonSerializer.Deserialize<Case>(text, Wire) : null;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private JsonObject Body(CaseRequest request) => new()
    {
        ["model"] = settings.Model,
        ["reasoning"] = new JsonObject { ["effort"] = settings.Effort },
        ["instructions"] = Prompt,
        ["input"] = $"Theme: {request.Theme}\nDifficulty: {request.Level.ToString().ToLowerInvariant()}",
        ["store"] = false,
        ["text"] = new JsonObject
        {
            ["format"] = new JsonObject
            {
                ["type"] = "json_schema",
                ["name"] = "mystery_case",
                ["strict"] = true,
                ["schema"] = JsonNode.Parse(Schema),
            },
        },
    };

    // The reply's output holds reasoning items and one message; the message's
    // content is either the case as text or a refusal.
    private static string? OutputText(JsonNode? reply) =>
        reply?["output"]?.AsArray()
            .Where(item => (string?)item?["type"] == "message")
            .SelectMany(item => item!["content"]?.AsArray() ?? [])
            .Where(part => (string?)part?["type"] == "output_text")
            .Select(part => (string?)part!["text"])
            .FirstOrDefault();

    private static string Resource(string name)
    {
        using var stream = typeof(OpenAiCaseWriterAccessor).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"{name} is not embedded.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
