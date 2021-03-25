using System;
using System.Device.Gpio;
using System.Threading;

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
            new Dht11().main();
        }
    }
}
