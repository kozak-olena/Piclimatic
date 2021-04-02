using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration _configuration;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly ILogger<TelegramBotHostedService> _logger;

        private TelegramBotClient _botClient;

        private Task _timedTurnOffNotificationHandler;

        private TelegramBotState _state = TelegramBotState.Default;
        private Guid _lastMessageIdentifier;

        private readonly Dictionary<TelegramBotState, (string Text, string CallbackData)[]> _stateButtons =
            new Dictionary<TelegramBotState, (string, string)[]>
            {
                { TelegramBotState.Default, new[] { ("Set thermostat", "initiateThermostat"), ("Turn on", "initiateTurnOn") } },
                { TelegramBotState.InitiatingThermostat, new[] { ("20 °C", "setThermostat20"), ("22 °C", "setThermostat22"), ("24 °C", "setThermostat24"), ("Cancel", "cancel") } },
                { TelegramBotState.ThermostatOn, new[] { ("Turn off", "turnOff")} },
                { TelegramBotState.InitiatingTurnOn, new[] { ("30 min", "turnOn30"), ("1 hour", "turnOn60"), ("2 hours", "turnOn120"), ("Cancel", "cancel") } },
                { TelegramBotState.TurnedOn, new[] { ("Turn off", "turnOff")} }
            };

        private readonly Dictionary<(TelegramBotState, TelegramBotState), string> _stateTransitionMessages =
            new Dictionary<(TelegramBotState, TelegramBotState), string>
            {
                { (TelegramBotState.Default, TelegramBotState.Default), "Choose desired operational mode." },
                { (TelegramBotState.Default, TelegramBotState.InitiatingThermostat), "Please set desired temperature (in °C) by selecting predefined option below or entering a custom value (from 18 to 30 °C)." },
                { (TelegramBotState.Default, TelegramBotState.InitiatingTurnOn), "Please set desired duration (in minutes) by selecting predefined option below or entering a custom value (from 1 to 120 minutes)." },
                { (TelegramBotState.InitiatingThermostat, TelegramBotState.Default), "Choose desired operational mode." },
                { (TelegramBotState.InitiatingThermostat, TelegramBotState.InitiatingThermostat), "Invalid input. Please set desired temperature (in °C) by selecting predefined option below or entering a custom value (from 18 to 30 °C)." },
                { (TelegramBotState.InitiatingThermostat, TelegramBotState.ThermostatOn), "Thermostat activated. Your desired temperature will be maintained automatically. To deactivate thermostat use a button below." },
                { (TelegramBotState.ThermostatOn, TelegramBotState.Default), "Thermostat deactivated. Choose desired operational mode." },
                { (TelegramBotState.InitiatingTurnOn, TelegramBotState.Default), "Choose desired operational mode." },
                { (TelegramBotState.InitiatingTurnOn, TelegramBotState.InitiatingTurnOn), "Invalid input. Please set desired duration (in minutes) by selecting predefined option below or entering a custom value (from 1 to 120 minutes)." },
                { (TelegramBotState.InitiatingTurnOn, TelegramBotState.TurnedOn), "Heating is turned on. It will stay on for desired duration. To turn the heating off use a button below." },
                { (TelegramBotState.TurnedOn, TelegramBotState.Default), "Heating is turned off. Choose desired operational mode." }
            };

        public TelegramBotHostedService
        (
            IEventHub eventHub,
            IConfiguration configuration,
            IHostApplicationLifetime applicationLifetime,
            ILogger<TelegramBotHostedService> logger
        )
        {
            _eventHub = eventHub;
            _configuration = configuration;
            _applicationLifetime = applicationLifetime;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var botToken = _configuration["botToken"];
            var ownerChatId = _configuration["chatId"];

            if (string.IsNullOrEmpty(botToken))
            {
                _logger.LogError("Telegram Bot token was not specified as command-line argument. Please make sure to include '--botToken \"...\"'.");

                _applicationLifetime.StopApplication();

                return Task.CompletedTask;
            }

            if (string.IsNullOrEmpty(ownerChatId))
            {
                if (long.TryParse(ownerChatId, out var ignored))
                {
                    _logger.LogWarning
                    (
                        "Owner chat id was not specified as command-line argument. " +
                        "Running in chat id discovery mode, no one will be able to use the bot. " +
                        "Please make sure to include '--chatId \"...\"'."
                    );
                }
                else
                {
                    _logger.LogError("Specified chat id is invalid. Valid 'long' value needs to be provided.");

                    _applicationLifetime.StopApplication();

                    return Task.CompletedTask;
                }
            }

            _timedTurnOffNotificationHandler = HandleTimedTurnOffNotifications();

            var botClient = new TelegramBotClient(botToken);

            botClient.OnMessage += BotClient_OnMessage;
            botClient.OnCallbackQuery += BotClient_OnCallbackQuery;

            botClient.StartReceiving();

            _botClient = botClient;

            return Task.CompletedTask;
        }

        private async Task HandleTimedTurnOffNotifications()
        {
            var cancellationToken = _applicationLifetime.ApplicationStopping;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await _eventHub.ReceiveTimedTurnOffNotificationMessage(cancellationToken);

                    await SendMessage(_state, TelegramBotState.Default, _configuration.GetValue<long>("chatId"), false);

                    _state = TelegramBotState.Default;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Stopped listening for events.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected failure.");
            }
        }

        private async void BotClient_OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            try
            {
                await _botClient.AnswerCallbackQueryAsync(e.CallbackQuery.Id);

                await HandleCallback(e.CallbackQuery.Data, e.CallbackQuery.From.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle callback query.");
            }
        }

        private async void BotClient_OnMessage(object sender, MessageEventArgs e)
        {
            try
            {
                await HandleMessage(e.Message.Text, e.Message.Chat.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle message.");
            }
        }

        private async Task HandleMessage(string text, long chatId)
        {
            if (!IsChatIdAuthorized(chatId))
            {
                await _botClient.SendTextMessageAsync
                (
                    chatId: chatId,
                    text: $"You are not authorized to use this bot. (chatId: {chatId})",
                    disableNotification: true
                );

                return;
            }

            bool resposeNeeded = true;

            TelegramBotState oldState = _state;

            if (_state == TelegramBotState.Default)
            {
                if (!text.Equals("/start", StringComparison.OrdinalIgnoreCase))
                {
                    resposeNeeded = false;
                }
            }
            else if (_state == TelegramBotState.InitiatingThermostat)
            {
                if (int.TryParse(text, out var desiredTemperature) && 18 <= desiredTemperature && desiredTemperature <= 30)
                {
                    _state = TelegramBotState.ThermostatOn;

                    _eventHub.PostTurnOnRequestedMessage(TurnOnRequestedMessage.CreateThermostatTurnOnRequestedMessage(desiredTemperature));
                }
            }
            else if (_state == TelegramBotState.ThermostatOn)
            {
                resposeNeeded = false;
            }
            else if (_state == TelegramBotState.InitiatingTurnOn)
            {
                if (int.TryParse(text, out var desiredDuration) && 1 <= desiredDuration && desiredDuration <= 120)
                {
                    _state = TelegramBotState.TurnedOn;

                    _eventHub.PostTurnOnRequestedMessage(TurnOnRequestedMessage.CreateTimedTurnOnRequestedMessage(TimeSpan.FromMinutes(desiredDuration)));
                }
            }
            else if (_state == TelegramBotState.TurnedOn)
            {
                resposeNeeded = false;
            }
            else
            {
                throw new NotSupportedException();
            }

            if (resposeNeeded)
            {
                await SendMessage(oldState, _state, chatId);
            }
        }

        private async Task HandleCallback(string data, long chatId)
        {
            if (!IsChatIdAuthorized(chatId))
            {
                return;
            }

            var dataParts = data.Split('_');

            if (!dataParts[1].Equals(_lastMessageIdentifier.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var command = dataParts[0];

            bool resposeNeeded = true;

            TelegramBotState oldState = _state;

            if (_state == TelegramBotState.Default)
            {
                if (command.Equals("initiateThermostat"))
                {
                    _state = TelegramBotState.InitiatingThermostat;
                }
                else if (command.Equals("initiateTurnOn"))
                {
                    _state = TelegramBotState.InitiatingTurnOn;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else if (_state == TelegramBotState.InitiatingThermostat)
            {
                if (command.Equals("setThermostat20"))
                {
                    _state = TelegramBotState.ThermostatOn;

                    _eventHub.PostTurnOnRequestedMessage(TurnOnRequestedMessage.CreateThermostatTurnOnRequestedMessage(20));
                }
                else if (command.Equals("setThermostat22"))
                {
                    _state = TelegramBotState.ThermostatOn;

                    _eventHub.PostTurnOnRequestedMessage(TurnOnRequestedMessage.CreateThermostatTurnOnRequestedMessage(22));
                }
                else if (command.Equals("setThermostat24"))
                {
                    _state = TelegramBotState.ThermostatOn;

                    _eventHub.PostTurnOnRequestedMessage(TurnOnRequestedMessage.CreateThermostatTurnOnRequestedMessage(24));
                }
                else if (command.Equals("cancel"))
                {
                    _state = TelegramBotState.Default;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else if (_state == TelegramBotState.InitiatingTurnOn)
            {
                if (command.Equals("turnOn30"))
                {
                    _state = TelegramBotState.TurnedOn;

                    _eventHub.PostTurnOnRequestedMessage(TurnOnRequestedMessage.CreateTimedTurnOnRequestedMessage(TimeSpan.FromMinutes(30)));
                }
                else if (command.Equals("turnOn60"))
                {
                    _state = TelegramBotState.TurnedOn;

                    _eventHub.PostTurnOnRequestedMessage(TurnOnRequestedMessage.CreateTimedTurnOnRequestedMessage(TimeSpan.FromMinutes(60)));
                }
                else if (command.Equals("turnOn120"))
                {
                    _state = TelegramBotState.TurnedOn;

                    _eventHub.PostTurnOnRequestedMessage(TurnOnRequestedMessage.CreateTimedTurnOnRequestedMessage(TimeSpan.FromMinutes(120)));
                }
                else if (command.Equals("cancel"))
                {
                    _state = TelegramBotState.Default;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else if (_state == TelegramBotState.ThermostatOn || _state == TelegramBotState.TurnedOn)
            {
                if (command.Equals("turnOff"))
                {
                    _state = TelegramBotState.Default;

                    _eventHub.PostTurnOffRequestedMessage(new TurnOffRequestedMessage());
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else
            {
                throw new NotSupportedException();
            }

            if (resposeNeeded)
            {
                await SendMessage(oldState, _state, chatId);
            }
        }

        private bool IsChatIdAuthorized(long chatId)
        {
            var requiredChatId = _configuration.GetValue<long>("chatId");

            return requiredChatId == chatId;
        }

        private async Task SendMessage(TelegramBotState oldState, TelegramBotState newState, long chatId, bool disableNotification = true)
        {
            _lastMessageIdentifier = Guid.NewGuid();

            var text = _stateTransitionMessages[(oldState, newState)];

            var buttons = _stateButtons[newState].Select(x => InlineKeyboardButton.WithCallbackData(x.Text, $"{x.CallbackData}_{_lastMessageIdentifier}"));

            await _botClient.SendTextMessageAsync
            (
                chatId: chatId,
                text: text,
                disableNotification: disableNotification,
                replyMarkup: new InlineKeyboardMarkup(buttons)
            );
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _botClient?.StopReceiving();

            await _timedTurnOffNotificationHandler;
        }
    }
}
