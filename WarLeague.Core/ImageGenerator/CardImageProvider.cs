using Microsoft.Extensions.Logging;
using WarLeague.Data.Data.Entities;
using WarLeague.Data.Repositories;

namespace WarLeague.Core.ImageGenerator
{
    public sealed class CardImageProvider
    {
        private readonly CardRepository _cards;
        private readonly YgoprodeckCardInfoClient _cardInfo;
        private readonly HttpClient _http;
        private readonly ILogger<CardImageProvider> _logger;

        public CardImageProvider(
            CardRepository cards,
            YgoprodeckCardInfoClient cardInfo,
            HttpClient http,
            ILogger<CardImageProvider> logger)
        {
            _cards = cards;
            _cardInfo = cardInfo;
            _http = http;
            _logger = logger;
        }

        /// <summary>
        /// Returns the raw image bytes for a card. If the card doesn't exist yet in
        /// the database, it's fetched and created first. Image bytes are downloaded
        /// and cached to the DB on first request. Passcode is matched against Card.YgoproId.
        /// </summary>
        public async Task<byte[]?> GetImageBytesAsync(int passcode, CancellationToken ct = default)
        {
            string passcodeStr = passcode.ToString();

            Card? card = await _cards.GetByYgoproIdAsync(passcodeStr, ct);

            if (card is null)
            {
                card = await CreateCardAsync(passcode, ct);

                if (card is null)
                    throw new Exception($"Card with id {passcode} was not found in DB and could not be created from API");
            }

            if (card.ImageData is not null)
                return card.ImageData;

            byte[]? downloaded = await DownloadImageAsync(passcode, ct);

            if (downloaded is null)
                return null;

            card.ImageData = downloaded;
            card.ImageContentType = "image/jpeg";

            await _cards.UpdateAsync(card, ct);

            return downloaded;
        }

        private async Task<Card?> CreateCardAsync(int passcode, CancellationToken ct)
        {
            CardInfoDto? info = await _cardInfo.GetByPasscodeAsync(passcode, ct);

            if (info is null)
            {
                _logger.LogWarning("No YGOPRODeck card info found for passcode {Passcode}", passcode);
                return null;
            }

            DateOnly releaseDate = ParseReleaseDate(info);

            var card = new Card
            {
                YgoproId = passcode.ToString(),
                Utf8Name = info.Name,
                FirstReleaseDate = releaseDate,
                BanlistEntries = Array.Empty<BanlistEntry>(),
            };

            await _cards.AddAsync(card, ct);

            return card;
        }

        private static DateOnly ParseReleaseDate(CardInfoDto info)
        {
            string? raw = info.MiscInfo?.FirstOrDefault()?.TcgDate;

            return DateOnly.TryParse(raw, out var parsed)
                ? parsed
                : DateOnly.FromDateTime(DateTime.UtcNow);
        }

        private async Task<byte[]?> DownloadImageAsync(int passcode, CancellationToken ct)
        {
            string url = $"https://images.ygoprodeck.com/images/cards/{passcode}.jpg";

            try
            {
                using var response = await _http.GetAsync(url, ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch card {Passcode}: {Status}", passcode, response.StatusCode);
                    return null;
                }

                return await response.Content.ReadAsByteArrayAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error downloading card {Passcode}", passcode);
                return null;
            }
        }
    }
}
