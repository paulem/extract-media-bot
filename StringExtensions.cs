namespace Telegram.ExtractMediaBot;

public static class StringExtensions
{
    public static bool IsDigitsOnly(this string inputString)
    {
        return inputString.All(c => c is >= '0' and <= '9');
    }
}