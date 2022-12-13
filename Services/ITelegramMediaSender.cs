using Telegram.Bot.Types;

namespace Telegram.ExtractMediaBot.Services;

public interface ITelegramMediaSender
{
    Task SendMedia(ChatId chatId, ExtractedMediaGroup mediaGroup);
}