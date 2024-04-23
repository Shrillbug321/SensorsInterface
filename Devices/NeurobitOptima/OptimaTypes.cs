namespace SensorsInterface.Devices;

public class OptimaTypes
{
	
}

public class TStartMeasurement
{
	private short DeviceContext;
	private MeasurementMode Mode;

	public enum MeasurementMode
	{
		Normal, Test
	}
}