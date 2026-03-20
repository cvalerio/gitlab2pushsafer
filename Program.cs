using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

builder.Services.AddOpenApi();
builder.Services.AddHttpClient();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/ci-status", async (HttpContext http, GitLabPipelineEvent payload, IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<Program> logger) =>
{
    var expectedToken = config["GitLab:WebhookToken"];
    var providedToken = http.Request.Headers["X-Gitlab-Token"].FirstOrDefault();

    if (string.IsNullOrEmpty(expectedToken) || providedToken != expectedToken)
    {
        logger.LogWarning("Unauthorized request: invalid or missing X-Gitlab-Token.");
        return Results.Unauthorized();
    }

    var status = payload.ObjectAttributes?.Status;
    var projectName = payload.Project?.Name;

    if (string.IsNullOrEmpty(status) || string.IsNullOrEmpty(projectName))
    {
        logger.LogWarning("Missing status or project name in payload.");
        return Results.BadRequest("Missing status or project name.");
    }

    var iconColor = status switch
    {
        "running" => "#add8e6",   // light blue
        "failed"  => "#ff0000",   // red
        "success" => "#008000",   // green
        _         => "#808080"    // grey for other statuses
    };

    var icon = status switch
    {
        "failed"  => "6",
        "success" => "20",
        _         => "4"
    };

    var authKey = config["Pushsafer:AuthKey"];
    var title = Uri.EscapeDataString($"{projectName} CI Status Update");
    var message = Uri.EscapeDataString($"The CI for project {projectName} has changed status: {status}");
    var url = $"https://www.pushsafer.com/api?k={authKey}&t={title}&m={message}&c={Uri.EscapeDataString(iconColor)}&i={icon}";
    // https://www.pushsafer.com/api?k=0Sz7KzTbHlkYfrSJdgrV&c=%2300ffff&t=dfgfdgfdgfgdg&m=dbeagrsa%20r%C2%A0%20df%20ss%20a%20f

    logger.LogInformation("Sending notification for project {Project} with status {Status}, url: {Url}", projectName, status, url);

    var client = httpClientFactory.CreateClient();
    var response = await client.GetAsync(url);

    if (!response.IsSuccessStatusCode)
    {
        logger.LogError("Pushsafer notification failed: {StatusCode}", response.StatusCode);
        return Results.Problem("Failed to send Pushsafer notification.");
    }

    logger.LogInformation("Notification sent for project {Project}, status {Status}", projectName, status);
    return Results.Ok();
});

app.Run();

public class GitLabPipelineEvent
{
    [JsonPropertyName("object_kind")]
    public string? ObjectKind { get; set; }

    [JsonPropertyName("object_attributes")]
    public PipelineAttributes? ObjectAttributes { get; set; }

    [JsonPropertyName("project")]
    public ProjectInfo? Project { get; set; }
}

public class PipelineAttributes
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("ref")]
    public string? Ref { get; set; }
}

public class ProjectInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
