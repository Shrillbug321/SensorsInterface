namespace SensorsInterface.Devices.NeurobitOptima;

public class TStartMeasurement
{
	private short DeviceContext;
	private MeasurementMode Mode;

	public enum MeasurementMode
	{
		Normal, Test
	}
}