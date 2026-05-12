namespace FilmAPI.DTO;

public sealed record SmtpConfig(
    string Host,
    int Port,
    string User,
    string Password,
    string From,
    bool UseSsl)
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) &&
                                !string.IsNullOrWhiteSpace(User) &&
                                !string.IsNullOrWhiteSpace(Password) &&
                                !string.IsNullOrWhiteSpace(From);
}
