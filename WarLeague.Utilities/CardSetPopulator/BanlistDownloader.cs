using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CardSetPopulator;

public class BanlistDownloader
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CardsetDownloader> _logger;

    public BanlistDownloader(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<CardsetDownloader> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task DownloadJson(CancellationToken cancellationToken)
    {
        var relativeJsonPath = _configuration.GetSection("Banlist")
            .GetValue<string>("RelativeJsonPath");
        if (string.IsNullOrEmpty(relativeJsonPath))
        {
            _logger.LogInformation("Missing Banlist:RelativeJsonPath, defaulted to banlist.json");
            relativeJsonPath = "banlist.json";
        }
        
        var date = _configuration.GetSection("Banlist").GetValue<string>("Date");
        if (string.IsNullOrEmpty(date))
        {
            // april 2014
            _logger.LogInformation("Missing Banlist:Date, defaulted to 2014-07-01");
            date = "2014-04-01";
        }

        const string baseUrl = "https://ygoprodeck.com/api/banlist/getBanList.php";
        var queryParams = new Dictionary<string, string?>
        {
            ["list"] = "TCG",
            ["date"] = date
        };

        await using var localFileStream = new FileStream(relativeJsonPath, FileMode.Create, FileAccess.Write);
        await using var jsonWriter = new Utf8JsonWriter(localFileStream, new JsonWriterOptions { Indented = true });
        jsonWriter.WriteStartArray();

        var fullUrl = QueryHelpers.AddQueryString(baseUrl, queryParams);
        var response = await _httpClient.GetAsync(fullUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(CancellationToken.None);
            throw new Exception($"Error downloading banlist:\n{response.StatusCode}\n{content}");
        }
        
        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, 
            new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            }, cancellationToken: cancellationToken);

        foreach (var banlistEntry in jsonDocument.RootElement.EnumerateArray())
        {
            var transformed = new JsonObject
            {
                ["id"] = banlistEntry.GetProperty("id").GetInt32(),
                ["name"] = banlistEntry.GetProperty("name").GetString(),
                ["maximum_allowed"] = banlistEntry.GetProperty("status_text").GetString() switch
                {
                    "Forbidden" => 0,
                    "Limited" => 1,
                    "Semi-Limited" => 2,
                    _ => throw new Exception("Status text from API not in expected range of Forbidden/Limited/Semi-Limited")
                }
            };
            transformed.WriteTo(jsonWriter);
        }
        jsonWriter.WriteEndArray();
        await jsonWriter.FlushAsync(cancellationToken);
        
        _logger.LogInformation("Banlist created");
    }
}