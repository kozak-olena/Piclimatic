using System;
using System.Device.Gpio;
using System.Threading;
using System.Threading.Tasks;

using Iot.Device.DHTxx;

namespace Piclimatic
{
    class Program
    {
        private static CancellationToken CancellationToken;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Piclimatic Started");

            var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken = cancellationTokenSource.Token;

            var dht11PollTask = PollDht11Continuously();
            var relayClickTask = ClickRelayContinuously();

            Console.WriteLine("Press Q to stop");
            do
            {
                while (!Console.KeyAvailable)
                {
                    await Task.Delay(50);
                }
            } while (Console.ReadKey(true).Key != ConsoleKey.Q);

            cancellationTokenSource.Cancel();

            await dht11PollTask;
            await relayClickTask;
        }

        private static async Task PollDht11Continuously()
        {
            try
            {
                using var dht11 = new Dht11(4);

                while (!CancellationToken.IsCancellationRequested)
                {
                    var humidity = dht11.Humidity;
                    var temperature = dht11.Temperature;

                    if (dht11.IsLastReadSuccessful)
                    {
                        Console.WriteLine($"H = {humidity}, T = {temperature}");
                    }
                    else
                    {
                        Console.WriteLine("Read failed.");
                    }

                    await Task.Delay(2000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static async Task ClickRelayContinuously()
        {
            var pin = 24;

            try
            {
                using var controller = new GpioController();
                controller.OpenPin(pin, PinMode.Output);

                while (!CancellationToken.IsCancellationRequested)
                {
                    controller.Write(pin, PinValue.Low);
                    await Task.Delay(1000);

                    controller.Write(pin, PinValue.High);
                    await Task.Delay(2000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static void RelayDemo()
        {
            var pin = 26;
            using var controller = new GpioController();
            controller.OpenPin(pin, PinMode.Output);


            controller.Write(pin, PinValue.Low);
            Thread.Sleep(1000);
            controller.Write(pin, PinValue.High);
            Thread.Sleep(1000);
        }
    }
}
