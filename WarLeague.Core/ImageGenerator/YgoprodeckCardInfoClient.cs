using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace WarLeague.Core.ImageGenerator
{
    public sealed class YgoprodeckCardInfoClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<YgoprodeckCardInfoClient> _logger;

        public YgoprodeckCardInfoClient(HttpClient http, ILogger<YgoprodeckCardInfoClient> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<CardInfoDto?> GetByPasscodeAsync(int passcode, CancellationToken ct = default)
        {
            string url = $"https://db.ygoprodeck.com/api/v7/cardinfo.php?id={passcode}&misc=yes";

            try
            {
                using var response = await _http.GetAsync(url, ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Card info lookup failed for {Passcode}: {Status}", passcode, response.StatusCode);
                    return null;
                }

                var payload = await response.Content.ReadFromJsonAsync<CardInfoResponse>(cancellationToken: ct);
                return payload?.Data?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error fetching card info for {Passcode}", passcode);
                return null;
            }
        }

        /// <summary>
        /// Fetches the entire card database in one call. Used for bulk seeding.
        /// </summary>
        public async Task<IReadOnlyList<CardInfoDto>> GetAllAsync(CancellationToken ct = default)
        {
            const string url = "https://db.ygoprodeck.com/api/v7/cardinfo.php";

            try
            {
                using var response = await _http.GetAsync(url, ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Bulk card info fetch failed: {Status}", response.StatusCode);
                    return Array.Empty<CardInfoDto>();
                }

                var payload = await response.Content.ReadFromJsonAsync<CardInfoResponse>(cancellationToken: ct);
                return payload?.Data ?? new List<CardInfoDto>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error fetching bulk card info");
                return Array.Empty<CardInfoDto>();
            }
        }
    }

    public sealed class CardInfoResponse
    {
        [JsonPropertyName("data")]
        public List<CardInfoDto>? Data { get; set; }
    }

    public sealed class CardInfoDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        // YGOPRODeck returns release date only on some card types (e.g. "2002-03-08");
        // it can be missing, hence nullable.
        [JsonPropertyName("misc_info")]
        public List<MiscInfoDto>? MiscInfo { get; set; }
    }

    public sealed class MiscInfoDto
    {
        [JsonPropertyName("tcg_date")]
        public string? TcgDate { get; set; }
    }
}
