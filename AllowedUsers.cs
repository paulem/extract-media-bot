namespace Telegram.ExtractMediaBot;

public class AllowedUsers
{
    public List<TelegramUser> Users { get; init; } = new();

    public bool IsAllowed(long userId)
    {
        return Users.FirstOrDefault(u => u.UserId == userId) is not null;
    }
}