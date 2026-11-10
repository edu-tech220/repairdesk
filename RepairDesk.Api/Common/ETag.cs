namespace RepairDesk.Api.Common;

/// <summary>Converts between a RowVersion byte array and the quoted base64 form used in If-Match/ETag headers.</summary>
public static class ETag
{
    public static string ToHeaderValue(byte[] rowVersion) => $"\"{Convert.ToBase64String(rowVersion)}\"";

    public static bool TryParse(string? headerValue, out byte[] rowVersion)
    {
        rowVersion = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return false;
        }

        var trimmed = headerValue.Trim().Trim('"');
        try
        {
            rowVersion = Convert.FromBase64String(trimmed);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
