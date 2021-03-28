namespace Piclimatic
{
    class TemperatureChangedMessage
    {
        public TemperatureChangedMessage(double oldTemperature, double newTemperature)
        {
            OldTemperature = oldTemperature;
            NewTemperature = newTemperature;
        }

        public double OldTemperature { get; }
        public double NewTemperature { get; }
    }
}
