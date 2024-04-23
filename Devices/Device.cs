namespace SensorsInterface.Devices;

public abstract class Device
{
	public virtual string DeviceName { get; set; } = "Device Name";
	public abstract List<string> SignalsNames { get; set; }
	public Dictionary<string, double> SignalsValues { get; } = [];
	
	protected abstract string DriverName { get; }
	protected abstract string DriverPath { get; }

	protected int DeviceContext { get; set; }
	
	protected Device()
	{
		foreach (string signalName in SignalsNames)
		{
			SignalsValues.Add(signalName, 0.0);
		}
	}
}