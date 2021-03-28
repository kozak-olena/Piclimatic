using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.ReplyMarkups;

namespace Piclimatic
{
    class TelegramBotHostedService : IHostedService
    {
        private readonly IEventHub _eventHub;
        private readonly ILogger<TelegramBotHostedService> _logger;

        private TelegramBotClient _botClient;

        public TelegramBotHostedService
        (
            IEventHub eventHub,
            ILogger<TelegramBotHostedService> logger
        )
        {
            _eventHub = eventHub;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var botClient = new TelegramBotClient(Program.BotToken);

            botClient.OnMessage += BotClient_OnMessage;
            botClient.OnCallbackQuery += BotClient_OnCallbackQuery;

            botClient.StartReceiving();

            _botClient = botClient;

            return Task.CompletedTask;
        }

        private async void BotClient_OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            try
            {
                await _botClient.AnswerCallbackQueryAsync(e.CallbackQuery.Id);

                string response;

                if (string.Equals(e.CallbackQuery.Data, "turnOn"))
                {
                    response = "Turned on";

                    _eventHub.PostRelayControlMessage(new RelayControlMessage(true));
                }
                else if (string.Equals(e.CallbackQuery.Data, "turnOff"))
                {
                    response = "Turned off";

                    _eventHub.PostRelayControlMessage(new RelayControlMessage(false));
                }
                else
                {
                    response = "Unknown action. Try again";
                }

                await _botClient.SendTextMessageAsync(chatId: e.CallbackQuery.From.Id, text: response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle callback query");
            }
        }

        private async void BotClient_OnMessage(object sender, MessageEventArgs e)
        {
            try
            {
                await SendMessage(null, e.Message.Chat.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle message");
            }
        }

        private async Task SendMessage(string additionalInfoForUser, long chatId)
        {
            var defaultText = "Choose action below";

            var text =
                string.IsNullOrEmpty(additionalInfoForUser)
                    ? string.Concat(additionalInfoForUser, ". ", defaultText)
                    : defaultText;

            await _botClient.SendTextMessageAsync
            (
                chatId: chatId,
                text: text,
                disableNotification: true,
                replyMarkup:
                    new InlineKeyboardMarkup
                    (
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Turn on", "turnOn"),
                            InlineKeyboardButton.WithCallbackData("Turn off", "turnOff"),
                        }
                    )
            );
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _botClient.StopReceiving();

            return Task.CompletedTask;
        }
    }
}
