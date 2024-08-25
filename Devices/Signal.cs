namespace SensorsInterface.Devices;

public class Signal
{
	public int Id { get; set; } = -1;
	public string Name { get; set; }
	public Dictionary<DateTime, double> Values { get; set; }
	public double Frequency { get; set; }
}