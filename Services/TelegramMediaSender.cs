using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.ExtractMediaBot.Services;

public class TelegramMediaSender : ITelegramMediaSender
{
    private readonly ITelegramBotClient _bot;
    private readonly IHttpClientFactory _httpClientFactory;

    public TelegramMediaSender(
        ITelegramBotClient bot,
        IHttpClientFactory httpClientFactory)
    {
        _bot = bot;
        _httpClientFactory = httpClientFactory;
    }

    public async Task SendMedia(ChatId chatId, ExtractedMediaGroup mediaGroup)
    {
        if (string.IsNullOrEmpty(mediaGroup.Text) && !mediaGroup.Media.Any())
            return;

        // If there is only text, send it as a text message

        if (!string.IsNullOrEmpty(mediaGroup.Text) && !mediaGroup.Media.Any())
        {
            await _bot.SendTextMessageAsync(chatId, mediaGroup.Text);
            return;
        }

        // If there is only one media element, send it using the appropriate method

        if (mediaGroup.Media.Count() == 1)
        {
            await SendSingleMedia(chatId, mediaGroup.Media.First(), mediaGroup.Text);
            return;
        }

        // Send media as an album

        await SendMediaGroup(chatId, mediaGroup);
    }

    private async Task SendSingleMedia(ChatId chatId, ExtractedMedia medium, string? caption)
    {
        var replyMarkup = new InlineKeyboardMarkup(
            new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Remove links", CallbackQueries.CmdMessageFormatCaption),
                    InlineKeyboardButton.WithCallbackData("Remove caption", CallbackQueries.CmdMessageRemoveCaption),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Detach caption", CallbackQueries.CmdMessageDetachCaption),
                    InlineKeyboardButton.WithCallbackData("It's OK", CallbackQueries.CmdMarkupRemoveKeyboard)
                },
            });

        switch (medium.Type)
        {
            case ExtractedMediaType.Photo:
                await _bot.SendChatActionAsync(chatId, ChatAction.UploadPhoto);
                await _bot.SendPhotoAsync(chatId, await CreateTelegramInputMedia(medium),
                    caption: caption,
                    replyMarkup: string.IsNullOrEmpty(caption) ? null : replyMarkup);
                break;
            case ExtractedMediaType.Video or ExtractedMediaType.AnimatedGif:
                await _bot.SendChatActionAsync(chatId, ChatAction.UploadVideo);
                await _bot.SendVideoAsync(chatId, await CreateTelegramInputMedia(medium),
                    caption: caption,
                    supportsStreaming: true,
                    replyMarkup: string.IsNullOrEmpty(caption) ? null : replyMarkup);
                break;
            default:
                throw new SendMediaException($"Unsupported media type: {medium.Type}.");
        }
    }

    private async Task SendMediaGroup(ChatId chatId, ExtractedMediaGroup mediaGroup)
    {
        var inputMedia = new List<IAlbumInputMedia>();

        foreach (var medium in mediaGroup.Media)
        {
            switch (medium.Type)
            {
                case ExtractedMediaType.Photo:
                    inputMedia.Add(new InputMediaPhoto(await CreateTelegramInputMedia(medium))
                    {
                        Caption = mediaGroup.Text
                    });
                    break;
                case ExtractedMediaType.Video or ExtractedMediaType.AnimatedGif:
                    inputMedia.Add(new InputMediaVideo(await CreateTelegramInputMedia(medium))
                    {
                        Caption = mediaGroup.Text,
                        SupportsStreaming = true
                    });
                    break;
            }
        }
        
        await _bot.SendMediaGroupAsync(chatId, inputMedia);
    }
    
    private async Task<InputMedia> CreateTelegramInputMedia(ExtractedMedia medium)
    {
        // https://core.telegram.org/bots/api#sending-files

        const int maxPhotoSizeMbUsingDirectUrl = 5;
        const int maxVideoSizeMbUsingDirectUrl = 20;

        const int maxPhotoSizeMbUsingStream = 10;
        const int maxVideoSizeMbUsingStream = 50;

        const int maxAudioSizeMbUsingStream = 50;

        int? mediaSizeMb;

        if (medium.SizeMb.HasValue)
            mediaSizeMb = medium.SizeMb.Value;
        else
        {
            var httpClient = _httpClientFactory.CreateClient();
            var responseMessage = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, medium.Url));
            mediaSizeMb = (int?)(responseMessage.Content.Headers.ContentLength / 1024 / 1024);
        }

        if (!mediaSizeMb.HasValue)
            throw new SendMediaException("Can't get media file size to upload.");

        switch (medium.Type)
        {
            case ExtractedMediaType.Photo:
                return mediaSizeMb switch
                {
                    < maxPhotoSizeMbUsingDirectUrl => new InputMedia(medium.Url),
                    < maxPhotoSizeMbUsingStream => new InputMedia(await _httpClientFactory.CreateClient().GetStreamAsync(medium.Url), medium.FileName),
                    _ => throw new SendMediaException("Media size exceeds the limit allowed for Telegram bot to upload.")
                };
            case ExtractedMediaType.Video:
            case ExtractedMediaType.AnimatedGif:
                return mediaSizeMb switch
                {
                    < maxVideoSizeMbUsingDirectUrl => new InputMedia(medium.Url),
                    < maxVideoSizeMbUsingStream => new InputMedia(await _httpClientFactory.CreateClient().GetStreamAsync(medium.Url), medium.FileName),
                    _ => throw new SendMediaException("Media size exceeds the limit allowed for Telegram bot to upload.")
                };
            case ExtractedMediaType.Audio:
                return mediaSizeMb switch
                {
                    < maxAudioSizeMbUsingStream => new InputMedia(await _httpClientFactory.CreateClient().GetStreamAsync(medium.Url), medium.FileName),
                    _ => throw new SendMediaException("Media size exceeds the limit allowed for Telegram bot to upload.")
                };
            default:
                throw new SendMediaException($"Unsupported media type: {medium.Type}.");
        }
    }
}