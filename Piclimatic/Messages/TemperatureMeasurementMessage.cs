namespace Piclimatic
{
    class TemperatureMeasurementMessage
    {
        public TemperatureMeasurementMessage(double temperature)
        {
            Temperature = temperature;
        }

        public double Temperature { get; }
    }
}
