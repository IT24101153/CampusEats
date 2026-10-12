using System.Net.Http.Headers;
using System.Net.Http.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});
builder.Services.AddHttpClient("Supabase", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("Frontend");

app.MapGet("/api/menu", async (IHttpClientFactory httpClientFactory, IConfiguration configuration) =>
{
    var result = await SupabaseRequestAsync(
        httpClientFactory,
        configuration,
        HttpMethod.Get,
        "meals?select=id,name,description,price,created_at&order=id");

    return Results.Content(result.Body, "application/json", statusCode: result.StatusCode);
});

app.MapPost("/api/menu", async (
    MealCreate meal,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) =>
{
    if (string.IsNullOrWhiteSpace(meal.name) ||
        string.IsNullOrWhiteSpace(meal.description) ||
        meal.price <= 0)
    {
        return Results.BadRequest(new { error = "Name, description, and a price greater than zero are required." });
    }

    var result = await SupabaseRequestAsync(
        httpClientFactory,
        configuration,
        HttpMethod.Post,
        "meals",
        new[] { meal },
        "return=representation");

    return Results.Content(result.Body, "application/json", statusCode: result.StatusCode);
});

app.Run();

static async Task<SupabaseResult> SupabaseRequestAsync(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    HttpMethod method,
    string path,
    object? body = null,
    string? prefer = null)
{
    var supabaseUrl = configuration["SUPABASE_URL"] ?? configuration["Supabase:Url"];
    var supabaseKey = configuration["SUPABASE_SECRET_KEY"];

    if (string.IsNullOrWhiteSpace(supabaseUrl) || string.IsNullOrWhiteSpace(supabaseKey))
    {
        return new SupabaseResult(
            StatusCodes.Status500InternalServerError,
            """{"error":"SUPABASE_URL and SUPABASE_SECRET_KEY must be configured as environment variables."}""");
    }

    using var request = new HttpRequestMessage(method, $"{supabaseUrl.TrimEnd('/')}/rest/v1/{path}");
    request.Headers.Add("apikey", supabaseKey);
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", supabaseKey);

    if (!string.IsNullOrWhiteSpace(prefer))
    {
        request.Headers.Add("Prefer", prefer);
    }

    if (body is not null)
    {
        request.Content = JsonContent.Create(body);
    }

    var client = httpClientFactory.CreateClient("Supabase");
    using var response = await client.SendAsync(request);
    var responseBody = await response.Content.ReadAsStringAsync();

    return new SupabaseResult((int)response.StatusCode, responseBody);
}

record MealCreate(string name, string description, decimal price);
record SupabaseResult(int StatusCode, string Body);
