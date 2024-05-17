using System.Windows.Media;
using SensorsInterface.Helpers;

namespace SensorsInterface.Devices;

public abstract class Device
{
	public virtual string DeviceName { get; set; } = "Device Name";
	public abstract List<string> AvailableSignals { get; set; }
	public abstract List<string> Signals { get; set; }
	public Dictionary<string, double> SignalsValues { get; } = [];

	public string StandardizedValue { get; protected set; } = "";
	
	public enum DeviceState
	{
		None, Loaded, Initialized, Working
	}

	private DeviceState state = DeviceState.None;

	public DeviceState State
	{
		get => state;
		set
		{
			state = value;
			StateColor = StateColors[value];
		}
	}

	public SolidColorBrush StateColor = new (Color.FromRgb(0, 128, 0));
	
	private static Dictionary<DeviceState, SolidColorBrush> StateColors = new()
	{
		{ DeviceState.None, new SolidColorBrush(Color.FromRgb(255, 0, 255)) },
		{ DeviceState.Loaded, new SolidColorBrush(Color.FromRgb(128, 128, 128)) },
		{ DeviceState.Initialized, new SolidColorBrush(Color.FromRgb(128, 128, 0)) },
		{ DeviceState.Working, new SolidColorBrush(Color.FromRgb(0, 128, 0)) },
	};
	
	protected abstract string DriverName { get; }
	protected abstract string DriverPath { get; }

	protected int DeviceContext { get; set; }
	
	protected Device()
	{
		foreach (string signalName in AvailableSignals)
		{
			SignalsValues.Add(signalName, 0.0);
		}
	}

	public abstract Error.ErrorCode Initialize();

	public abstract string Read();

	public abstract string StandardizeValue();

	public abstract void Close();
}