using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using SensorsInterface.Devices;
using SensorsInterface.Devices.NeurobitOptima;
using SensorsInterface.Native;
using static SensorsInterface.Helpers.Error;

namespace SensorsInterface;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
	public record DeviceListBoxElement
	{
		public Device Device;
		public SolidColorBrush StateColor;
	}
	
	private List<Device> Devices = [];

	private List<DeviceListBoxElement> DeviceListBoxElements = new();

	private NamedPipeServerStream pipe;
	private StreamWriter pipeWriter;
	
	public MainWindow()
	{
		InitializeComponent();
		/*Devices.Add(new NeurobitOptima());
		
		DevicesListBox.DataContext = Devices;
		DevicesListBox.ItemsSource = Devices;*/

		//CreatePipe();
		
		Devices.Add(new NeurobitOptima());
		GetConnectedDevices();
		InitializeDevices();
		
		/*DevicesListBox.DataContext = Devices;
		DevicesListBox.ItemsSource = Devices;*/
		
		/*Task.Run(() =>
		{
			while (true)
			{
				GetConnectedDevices();
                InitializeDevices();
				ReadFromDevices();
				SendToVR();
			}
		});*/
	}

	private void CreatePipe()
	{
		pipe = new NamedPipeServerStream("VRTPipe");
		pipeWriter = new StreamWriter(pipe);
		pipe.WaitForConnection();
	}
	
	private void GetConnectedDevices()
	{
		List<USBDevice> usbDevices = USBDevice.GetUSBDevices();
		foreach (USBDevice usbDevice in usbDevices)
		{
			Device? device = usbDevice.Description switch
			{
				//nazwy tymczasowe todo
				"Neurobit Optima" => new NeurobitOptima(),
				_ => null
			};
			if (device != null && !Devices.Contains(device))
				Devices.Add(device);
		}
		FoundDeviceVisibility.Visibility = Devices.Count == 0 ? Visibility.Visible : Visibility.Hidden;
	}
	
	private void InitializeDevices(bool onlyNew = true)
	{
		List<Device> devices = onlyNew ? Devices.Where(d => d.State == Device.DeviceState.None).ToList() : Devices;
		foreach (Device device in devices)
		{
			device.State = Device.DeviceState.Loaded;
			ErrorCode errorCode = device.Initialize();
			if (errorCode > 0)
			{
				ShowMessageBox(errorCode, device.DeviceName);
				continue;
			}

			device.State = Device.DeviceState.Initialized;
			device.StateColor = new SolidColorBrush(Color.FromRgb(0, 128, 0));
		}
	}

	private void ReadFromDevices()
	{
		foreach (Device device in Devices)
		{
			Console.WriteLine(device.Read());
			Console.WriteLine(device.StandardizeValue());
		}
	}

	private void SendToVR()
	{
		foreach (Device device in Devices)
		{
			pipeWriter.WriteLine(device.StandardizedValue);
		}
	}
}