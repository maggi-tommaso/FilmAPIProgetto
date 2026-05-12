namespace FilmAPI.Services;

public static class RedirectUrlValidator
{
    public static bool IsValidRelativePath(string? redirect)
    {
        if (string.IsNullOrWhiteSpace(redirect))
            return false;

        if (redirect.StartsWith('/') && !redirect.StartsWith("//"))
            return true;

        return false;
    }

    public static string SanitizeRedirect(string? redirect, string fallbackPath)
    {
        return IsValidRelativePath(redirect) ? redirect.Trim() : fallbackPath;
    }
}
