using System;
using System.Device.Gpio;
using System.Threading;

using Iot.Device.DHTxx;

namespace Piclimatic
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            //int pin = 37;
            //using var controller = new GpioController();
            //controller.OpenPin(pin, PinMode.Output);
            //
            //controller.Write(pin, PinValue.High);
            //Thread.Sleep(1000);
            //controller.Write(pin, PinValue.Low);
            //Thread.Sleep(1000);
            using var dht11 = new Dht11(4);

            for (var i = 0; i < 10; i++)
            {
                var humidity = dht11.Humidity;
                var temperature = dht11.Temperature;

                if (dht11.IsLastReadSuccessful)
                {
                    Console.WriteLine($"H = {dht11.Humidity}, T = {dht11.Temperature}");
                }
                else
                {
                    Console.WriteLine("Read failed.");
                }

                Thread.Sleep(2000);
            }
        }
    }
}
