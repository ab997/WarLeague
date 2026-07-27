using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace WarLeague.Core.ImageGenerator
{
    public sealed class DeckImageService
    {
        private const int CardWidth = 120;
        private const int CardHeight = 175;
        private const int Columns = 10;
        private const int Margin = 20;
        private const int Gap = 20;

        private static readonly SKSamplingOptions Sampling =
            new(SKFilterMode.Linear, SKMipmapMode.None);

        private readonly CardImageProvider _images;
        private readonly ILogger<DeckImageService> _logger;

        public DeckImageService(CardImageProvider images, ILogger<DeckImageService> logger)
        {
            _images = images;
            _logger = logger;
        }

        public async Task<MemoryStream> RenderAsync(Deck deck, CancellationToken ct = default)
        {
            List<int> allCards = [];
            allCards.AddRange(deck.Main);
            allCards.AddRange(deck.Side);
            allCards.AddRange(deck.Extra);
            HashSet<int> hs = allCards.ToHashSet();

            await _images.LoadCacheAsync(hs);
            int mainRows = Math.Max(1, (int)Math.Ceiling(deck.Main.Count / (double)Columns));
            int extraRows = Math.Max(1, (int)Math.Ceiling(deck.Extra.Count / (double)Columns));
            int sideRows = Math.Max(1, (int)Math.Ceiling(deck.Side.Count / (double)Columns));

            int width = Margin * 2 + Columns * CardWidth;
            int height =
                Margin * 2 +
                mainRows * CardHeight + Gap +
                extraRows * CardHeight + Gap +
                sideRows * CardHeight;

            using var bitmap = new SKBitmap(width, height);
            using var canvas = new SKCanvas(bitmap);

            canvas.Clear(SKColors.DarkSlateGray);

            int y = Margin;

            await DrawSectionAsync(canvas, deck.Main, y, ct);
            y += mainRows * CardHeight + Gap;

            await DrawSectionAsync(canvas, deck.Extra, y, ct);
            y += extraRows * CardHeight + Gap;

            await DrawSectionAsync(canvas, deck.Side, y, ct);

            canvas.Flush();

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);

            var stream = new MemoryStream();
            data.SaveTo(stream);
            stream.Position = 0;

            return stream;
        }

        private async Task DrawSectionAsync(SKCanvas canvas, IReadOnlyList<int> cards, int startY, CancellationToken ct)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                byte[]? bytes = await _images.GetImageBytesAsync(cards[i], ct);

                if (bytes is null)
                    continue;

                using var data = SKData.CreateCopy(bytes);
                using var image = SKImage.FromEncodedData(data);

                if (image is null)
                {
                    _logger.LogWarning("Failed to decode cached image for {Passcode}", cards[i]);
                    continue;
                }

                int x = Margin + (i % Columns) * CardWidth;
                int y = startY + (i / Columns) * CardHeight;

                var dest = new SKRect(x, y, x + CardWidth, y + CardHeight);

                canvas.DrawImage(image, dest, Sampling);
            }
        }
    }
}
