namespace SensorsInterface.Native;

using System.Management;

public class USBDevice(string deviceID, string pnpDeviceID, string description)
{
	public string DeviceID = deviceID;
	public string PnpDeviceID = pnpDeviceID;
	public string Description = description;

	public static List<USBDevice> GetUSBDevices()
	{
		List<USBDevice> devices = [];

		using var searcher = new ManagementObjectSearcher(
			@"Select * From Win32_PnPEntity ");
		using ManagementObjectCollection collection = searcher.Get();

		foreach (var device in collection)
		{
			if (device.GetPropertyValue("Description") != null &&
			    ((string)device.GetPropertyValue("Description")).Contains("USB"))
				devices.Add(new USBDevice(
					(string)device.GetPropertyValue("DeviceID"),
					(string)device.GetPropertyValue("PNPDeviceID"),
					(string)device.GetPropertyValue("Description")
				));
		}

		return devices;
	}
}