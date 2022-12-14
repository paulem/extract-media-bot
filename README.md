<br/>
<img alt="Extract Media Bot" src="https://user-images.githubusercontent.com/2874236/207578176-063a298d-d315-4bbe-b4d2-6784ab7565a2.svg" width="350" />

## Extract Media Telegram Bot

Extract Media Bot is a Telegram bot that extracts media (images, videos) from posts. You send a link to a post, the bot returns a set of media attachments in response.
>Currently, only extracting media from tweets is supported.

## Extensions

To add support for a new service, just implement the `IMediaExtractor` interface.

## Be ready to setup a webhook

This bot uses the webhook method to get updates from Telegram. There are some advantages of using a webhook over getUpdates. As soon as an update arrives, Telegram delivers it to your bot for processing.

Pros:
* Avoids your bot having to ask for updates frequently
* Avoids the need for some kind of polling mechanism in your code

Cons:
* Much harder to setup

Read more about Telegram webhook here:

- [Marvin's Marvellous Guide to All Things Webhook](https://core.telegram.org/bots/webhooks)
- [Getting updates](https://core.telegram.org/bots/api#getting-updates)

## Variables

The bot requires a number of variables to work. For local run they can be set using [user secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) or as environment variables. To run the bot in Docker, you need to set the variables in the `.env` file.

| Variable                         | Description                                                                                                                                      |
|----------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------|
| `Telegram__BotToken`             | Telegram Bot Token. Use [@BotFather](https://t.me/botfather) to obtain it                                                                        |
| `Telegram__HostAddress`          | HTTPS URL that Telegram will use to send updates                                                                                                 |
|                                  |                                                                                                                                                  |
| `AllowedUsers__Users__0__UserId` | Telegram ID of the user who will be able to interact with the bot. To get your ID, message [@userinfobot](https://t.me/@userinfobot) on Telegram |
| ...                              | ...                                                                                                                                              |
| `AllowedUsers__Users__N__UserId` | You can set multiple bot user IDs by incrementing the number                                                                                     |
| `Twitter__ApiToken`              | Twitter API App-only Token. [Here](https://developer.twitter.com/en/docs/authentication/oauth-2-0/bearer-tokens) is how to obtain it             |

If you are going to use `user secrets` for local run, you need to replace the double underscore `__` with a colon `:`.
In any other case, use double underscores.

>The `:` separator doesn't work with environment variable hierarchical keys on all platforms. The double underscore `__` is supported by all platforms.

## How to run locally

Please make sure you have .NET 6 or newer installed. You can download .NET runtime from the [official site.](https://dotnet.microsoft.com/download)
This is a short description how you can run and debug your bot locally. The description presumes that you already have a bot and its token. If not, please follow [this guide](https://core.telegram.org/bots/tutorial#obtain-your-bot-token) to create a bot and get the token.

### Ngrok

For the bot to work, in addition to the `BotToken`, you need the `HTTPS URL` that Telegram will use to send updates. This is where the [ngrok](https://ngrok.com) service comes in.

Ngrok gives you the opportunity to access your local machine from a temporary subdomain provided by ngrok. This domain can later send to the telegram API as URL for the webhook.
Download [ngrok](https://ngrok.com/download) and start it on port 8443.

```shell
ngrok http 8443 
```

Ports currently supported by Telegram for webhooks are 443, 80, 88 and 8443. If you want to use another supported port, start ngrok using this port and change the port in `launchSettings.json`.

From ngrok you'll get a temporary public https URL pointing to your local server.

### Set Variables

Get all the other variables listed in the table above and set them either as user secrets or environment variables.
Below is how to add variables using user secrets.

Run the following command from the directory in which the project file exists:

```shell
dotnet user-secrets set "Telegram:BotToken" "<YOUR_BOT_TOKEN>"
```

Repeat the command for all needed variables.

### Run Bot

Now you can start the Bot as a local instance. Make sure that the ngrok port matches the port that the locally running bot will listen on. This port is set in `launchSettings.json`.

Now your bot should answer from every message you send to it.

## How to run in Docker

To run in Docker, you need to set the values of the variables in the `.env` file and place the SSL certificate on the host machine.

>The file `docker-compose.yml` uses the host path to the certificate `/root/.aspnet/https`

### Set Variables

Open the `.env.example` file with a text editor and fill in variable values. Save file as `.env`.
> Make sure that you save the file with the name `.env`.

Your `Telegram__HostAddress` variable must consist of the HTTPS URL of your server with the specified port.
```
TelegramBot__HostAddress=https://mydomain:8443
```

### Obtain SSL Certificate

Get an SSL certificate, place the `.crt` and `.key` files on the host machine.

### Run Bot

Run the following commands from the directory in which the project file exists:

```shell
docker compose build
docker compose up -d
```
