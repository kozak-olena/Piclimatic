using System;
using System.Collections.Generic;
using System.Linq;
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
                { (TelegramBotState.Default, TelegramBotState.InitiatingThermostat), "Please set desired temperature (in °C) by selecting predefined option below or entering a custom value (from 18 to 26 °C)." },
                { (TelegramBotState.Default, TelegramBotState.InitiatingTurnOn), "Please set desired duration (in minutes) by selecting predefined option below or entering a custom value (from 1 to 120 minutes)." },
                { (TelegramBotState.InitiatingThermostat, TelegramBotState.Default), "Choose desired operational mode." },
                { (TelegramBotState.InitiatingThermostat, TelegramBotState.InitiatingThermostat), "Invalid input. Please set desired temperature (in °C) by selecting predefined option below or entering a custom value (from 18 to 26 °C)." },
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

                await HandleCallback(e.CallbackQuery.Data, e.CallbackQuery.From.Id);
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
                await HandleMessage(e.Message.Text, e.Message.Chat.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle message");
            }
        }

        private async Task HandleMessage(string text, long chatId)
        {
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
                if (int.TryParse(text, out var desiredTemperature) && 18 <= desiredTemperature && desiredTemperature <= 26)
                {
                    //TODO: store desired temperature
                    _state = TelegramBotState.ThermostatOn;
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
                    //TODO: store desired duration
                    _state = TelegramBotState.TurnedOn;
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
                _lastMessageIdentifier = Guid.NewGuid();

                await SendMessage(oldState, _state, chatId, _lastMessageIdentifier);
            }
        }

        private async Task HandleCallback(string data, long chatId)
        {
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
                    //TODO: store desired temperature
                    _state = TelegramBotState.ThermostatOn;
                }
                else if (command.Equals("setThermostat22"))
                {
                    //TODO: store desired temperature
                    _state = TelegramBotState.ThermostatOn;
                }
                else if (command.Equals("setThermostat24"))
                {
                    //TODO: store desired temperature
                    _state = TelegramBotState.ThermostatOn;
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
                    //TODO: store desired duration
                    _state = TelegramBotState.TurnedOn;
                }
                else if (command.Equals("turnOn60"))
                {
                    //TODO: store desired duration
                    _state = TelegramBotState.TurnedOn;
                }
                else if (command.Equals("turnOn120"))
                {
                    //TODO: store desired duration
                    _state = TelegramBotState.TurnedOn;
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
                _lastMessageIdentifier = Guid.NewGuid();

                await SendMessage(oldState, _state, chatId, _lastMessageIdentifier);
            }
        }

        private async Task SendMessage(TelegramBotState oldState, TelegramBotState newState, long chatId, Guid messageIdentifier)
        {
            var text = _stateTransitionMessages[(oldState, newState)];

            var buttons = _stateButtons[newState].Select(x => InlineKeyboardButton.WithCallbackData(x.Text, $"{x.CallbackData}_{messageIdentifier}"));

            await _botClient.SendTextMessageAsync
            (
                chatId: chatId,
                text: text,
                disableNotification: true,
                replyMarkup: new InlineKeyboardMarkup(buttons)
            );
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _botClient.StopReceiving();

            return Task.CompletedTask;
        }
    }
}
