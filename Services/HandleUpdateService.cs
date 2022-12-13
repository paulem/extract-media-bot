using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.ExtractMediaBot.Extractors;

namespace Telegram.ExtractMediaBot.Services;

public class HandleUpdateService
{
    private readonly ITelegramBotClient _bot;
    private readonly IMediaExtractor _mediaExtractor;
    private readonly ITelegramMediaSender _mediaSender;
    private readonly AllowedUsers _allowedUsers;
    private readonly MediaSenderConfiguration _configuration;
    private readonly ILogger<HandleUpdateService> _logger;

    public HandleUpdateService(
        ITelegramBotClient bot,
        IMediaExtractor mediaExtractor,
        ITelegramMediaSender mediaSender,
        IOptionsSnapshot<MediaSenderConfiguration> configuration,
        IOptionsSnapshot<AllowedUsers> allowedUsers,
        ILogger<HandleUpdateService> logger)
    {
        _bot = bot;
        _mediaExtractor = mediaExtractor;
        _mediaSender = mediaSender;
        _allowedUsers = allowedUsers.Value;
        _configuration = configuration.Value;
        _logger = logger;
    }

    public async Task EchoAsync(Update update)
    {
        var handler = update.Type switch
        {
            UpdateType.Message => BotOnMessageReceived(update.Message!),
            UpdateType.EditedMessage => BotOnMessageReceived(update.EditedMessage!),
            UpdateType.CallbackQuery => BotOnCallbackQueryReceived(update.CallbackQuery!),
            _ => UnknownUpdateHandlerAsync(update)
        };

        try
        {
            await handler;
        }
        catch (ExtractionException ex)
        {
            await _bot.SendTextMessageAsync(update.Message!.Chat.Id, ex.Message.TrimEnd('.'));
        }
        catch (SendMediaException ex)
        {
            await _bot.SendTextMessageAsync(update.Message!.Chat.Id, ex.Message.TrimEnd('.'));
        }
        catch (Exception ex)
        {
            switch (ex)
            {
                case ApiRequestException apiEx:
                    _logger.LogError(ex, "Telegram API Error: [{ErrorCode}] {Message}", apiEx.ErrorCode, apiEx.Message);
                    break;
                default:
                    _logger.LogError(ex, "Unknown exception occurred");
                    break;
            }

            await _bot.SendTextMessageAsync(update.Message!.Chat.Id, "Something is wrong");
        }
    }

    private async Task BotOnMessageReceived(Message message)
    {
        if (message.From is null || !_allowedUsers.IsAllowed(message.From.Id))
        {
            await _bot.SendTextMessageAsync(message.Chat.Id, "Unauthorized user");
            return;
        }
        
        if (message.Type != MessageType.Text || string.IsNullOrEmpty(message.Text))
            return;

        var media = await _mediaExtractor.ExtractAsync(message.Text);

        if (media is null)
            await _bot.SendTextMessageAsync(message.Chat.Id, "No media found");
        else
        {
            await _mediaSender.SendMedia(message.Chat.Id, media);
            
            if (_configuration.DeleteInputMessageAfterMediaSent)
                await _bot.DeleteMessageAsync(message.Chat.Id, message.MessageId);
        }
    }

    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery)
    {
        // Handle message caption

        string? FormatMessageCaption(string text)
        {
            return Regex.Replace(text, @"http[^\s]+", string.Empty);
        }

        var msg = callbackQuery.Message!;

        switch (callbackQuery.Data)
        {
            case CallbackQueries.CmdMarkupRemoveKeyboard:
                await _bot.EditMessageReplyMarkupAsync(msg.Chat.Id, msg.MessageId);
                break;
            case CallbackQueries.CmdMessageRemoveCaption:
                await _bot.EditMessageCaptionAsync(msg.Chat.Id, msg.MessageId, null);
                break;
            case CallbackQueries.CmdMessageDetachCaption:
                await _bot.EditMessageCaptionAsync(msg.Chat.Id, msg.MessageId, null);
                if (!string.IsNullOrEmpty(msg.Caption))
                {
                    var replyMarkup = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Remove links", CallbackQueries.CmdMessageFormatCaption),
                            InlineKeyboardButton.WithCallbackData("It's OK", CallbackQueries.CmdMarkupRemoveKeyboard)
                        }
                    });

                    await _bot.SendTextMessageAsync(msg.Chat.Id, msg.Caption,
                        replyToMessageId: msg.MessageId,
                        replyMarkup: replyMarkup,
                        disableWebPagePreview: true);
                }

                break;

            case CallbackQueries.CmdMessageFormatCaption:
                if (!string.IsNullOrEmpty(msg.Caption))
                {
                    var replyMarkup = new InlineKeyboardMarkup(
                        new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Remove caption", CallbackQueries.CmdMessageRemoveCaption),
                                InlineKeyboardButton.WithCallbackData("Detach caption", CallbackQueries.CmdMessageDetachCaption),
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("It's OK",
                                    CallbackQueries.CmdMarkupRemoveKeyboard)
                            },
                        });

                    await _bot.EditMessageCaptionAsync(msg.Chat.Id, msg.MessageId,
                        FormatMessageCaption(msg.Caption),
                        replyMarkup: replyMarkup);
                }

                break;
        }

        await _bot.AnswerCallbackQueryAsync(callbackQuery.Id);
    }

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }
}