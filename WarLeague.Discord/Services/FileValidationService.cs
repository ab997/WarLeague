namespace WarLeague.Discord.Services;

public class FileValidationService
{
    public bool IsValidYdkFile(string filename, string? url = null)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            return false;
        }

        return filename.EndsWith(".ydk", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsValidReplayUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
