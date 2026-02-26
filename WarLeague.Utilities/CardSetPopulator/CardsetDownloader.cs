using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CardSetPopulator;

public class CardsetDownloader
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CardsetDownloader> _logger;

    public CardsetDownloader(
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
        var relativeJsonPath = _configuration.GetSection("CardSet")
            .GetValue<string>("RelativeJsonPath");
        if (string.IsNullOrEmpty(relativeJsonPath))
        {
            _logger.LogInformation("Missing CardSet:RelativeJsonPath, defaulted to cardset.json");
            relativeJsonPath = "cardset.json";
        }
        var endDate = _configuration.GetSection("CardSet").GetValue<string>("EndDate");
        if (string.IsNullOrEmpty(endDate))
        {
            // shogi rook promo
            _logger.LogInformation("Missing CardSet:EndDate, defaulted to 2014-07-01");
            endDate = "2014-07-01";
        }

        const string baseUrl = "https://ygoprodeck.com/api/search/cards.php";
        var queryParams = new Dictionary<string, string?>
        {
            ["format"] = "tcg",
            // maximum limit supported by ygopro
            ["num"] = "100",
            ["offset"] = "0",
            ["enddate"] = endDate
        };
        await using var localFileStream = new FileStream(relativeJsonPath, FileMode.Create, FileAccess.Write);
        await using var jsonWriter = new Utf8JsonWriter(localFileStream, new JsonWriterOptions { Indented = true });
        jsonWriter.WriteStartArray();

        while (!cancellationToken.IsCancellationRequested)
        {
            var fullUrl = QueryHelpers.AddQueryString(baseUrl, queryParams);      
            var response = await _httpClient.GetAsync(fullUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(CancellationToken.None);
                throw new Exception($"Error downloading cardset:\n{response.StatusCode}\n{content}");
            }
            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var jsonDocument = await JsonDocument.ParseAsync(contentStream, 
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                }, cancellationToken: cancellationToken);
            
            var cardsArray = jsonDocument.RootElement.GetProperty("cards");
            foreach (var cardJsonElement in cardsArray.EnumerateArray())
            {
                var transformed = new JsonObject
                {
                    ["id"] = cardJsonElement.GetProperty("id").GetInt32(),
                    ["name"] = cardJsonElement.GetProperty("name").GetString(),
                    ["release_date"] = cardJsonElement.GetProperty("tcg_date").GetDateTimeOffset().Date
                };
                transformed.WriteTo(jsonWriter);
            }
            var pagingInfo = jsonDocument.RootElement.GetProperty("paging");
            var pagesRemaining = pagingInfo.GetProperty("pages_remaining").GetInt32();
            if (pagesRemaining == 0)
                break;
            var nextPage = pagingInfo.GetProperty("next_page_offset").GetInt32();
            queryParams["offset"] = nextPage.ToString();
        }
        
        jsonWriter.WriteEndArray();
        await jsonWriter.FlushAsync(cancellationToken);
        
        _logger.LogInformation("Cardset created");
    }
}