using Serilog;
using Telegram.Bot;
using Telegram.ExtractMediaBot;
using Telegram.ExtractMediaBot.Extractors.Instagram;
using Telegram.ExtractMediaBot.Extractors.Twitter;
using Telegram.ExtractMediaBot.Services;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting up");

try
{
    var builder = WebApplication.CreateBuilder(args);
    
    builder.Host.UseSerilog((ctx, lc) => lc
        .WriteTo.Console()
        .ReadFrom.Configuration(ctx.Configuration));
    
    var botConfig = builder.Configuration.GetSection("TelegramBot");
    var twitterConfig = builder.Configuration.GetSection("Twitter");
    var instagramConfig = builder.Configuration.GetSection("Instagram");

    builder.Services.Configure<BotConfiguration>(botConfig);
    builder.Services.Configure<MediaSenderConfiguration>(builder.Configuration.GetSection("TelegramMediaSender"));
    builder.Services.Configure<AllowedUsers>(builder.Configuration.GetSection("AllowedUsers"));
    
    builder.Services.AddHostedService<ConfigureWebhook>();
    
    builder.Services.AddHttpClient("TelegramBot")
        .AddTypedClient<ITelegramBotClient>(httpClient => new TelegramBotClient(botConfig.Get<BotConfiguration>().BotToken, httpClient));
    
    // Extractors

    builder.Services.AddHttpClient("Twitter")
        .AddTypedClient(httpClient => new TweetMediaExtractor(twitterConfig.Get<TwitterConfiguration>().ApiToken, httpClient));
    
    builder.Services.AddHttpClient("Instagram")
        .AddTypedClient(httpClient => new InstagramMediaExtractor(instagramConfig.Get<InstagramConfiguration>().ApiToken, httpClient));

    builder.Services.AddTransient<IMediaExtractor>(m =>
        new CompositeMediaExtractor(
            new IMediaExtractor[]
            {
                m.GetRequiredService<TweetMediaExtractor>(),
                m.GetRequiredService<InstagramMediaExtractor>()
            }));

    builder.Services.AddTransient<ITelegramMediaSender, TelegramMediaSender>();
    builder.Services.AddScoped<HandleUpdateService>();

    // The Telegram.Bot library heavily depends on Newtonsoft.Json library to deserialize
    // incoming webhook updates and send serialized responses back.
    builder.Services.AddControllers().AddNewtonsoftJson();

    var app = builder.Build();

    app.UseRouting();
    app.UseCors();
    
    app.UseSerilogRequestLogging();

    app.UseEndpoints(endpoints =>
    {
        var token = botConfig.Get<BotConfiguration>().BotToken;
        endpoints.MapControllerRoute(name: "tgwebhook",
            pattern: $"bot/{token}",
            new { controller = "Webhook", action = "Post" });
        endpoints.MapControllers();
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}