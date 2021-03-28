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
    public class TelegramBotHostedService : IHostedService
    {
        private readonly ISynchronizer _synchronizer;
        private readonly ILogger<TelegramBotHostedService> _logger;

        private TelegramBotClient _botClient;

        public TelegramBotHostedService
        (
            ISynchronizer synchronizer,
            ILogger<TelegramBotHostedService> logger
        )
        {
            _synchronizer = synchronizer;
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

                    _synchronizer.SignalRelay(true);
                }
                else if (string.Equals(e.CallbackQuery.Data, "turnOff"))
                {
                    response = "Turned off";

                    _synchronizer.SignalRelay(false);
                }
                else
                {
                    response = "Unknown action. Try again.";
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
                await _botClient.SendTextMessageAsync
                (
                    chatId: e.Message.Chat,
                    text: "Choose action below",
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle message");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _botClient.StopReceiving();

            return Task.CompletedTask;
        }
    }
}
