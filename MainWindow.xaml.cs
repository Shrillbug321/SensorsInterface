using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SensorsInterface.Devices;
using SensorsInterface.Devices.NeurobitOptima;
using SensorsInterface.Helpers;
using SensorsInterface.Native;
using static SensorsInterface.Devices.Device;
using static SensorsInterface.Helpers.Error;

namespace SensorsInterface;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{

	private List<Device> Devices = [];
	private List<Device> workingDevices = [];

	/*private NamedPipeServerStream? pipe;
	private StreamWriter pipeWriter;*/

	private SendMessageDelegate? sendMessage;
	private Global global = new();

	public MainWindow()
	{
		CultureInfo ci = new CultureInfo("en-US");
		Thread.CurrentThread.CurrentCulture = ci;
		Thread.CurrentThread.CurrentUICulture = ci;
		InitializeComponent();

		Devices.Add(new NeurobitOptima());
		/*Devices.Add(new NeurobitOptima());
		Devices[1].Name = "kkkkkkkkkkkkkkkkkkkkk";*/
		GetConnectedDevices();
		InitializeDevices();
		Closing += (_, _) =>
		{
			foreach (Device device in Devices)
			{
				device.Close();
			}
		};

		CreateHUD();
		CreateHUDNeurobitOptima();

		Task.Run(() =>
		{
			while (true)
			{
				GetConnectedDevices();
				InitializeDevices();
				FindWorkingDevices();
				RetrieveFromDevices();
				RefreshChannelValues();
				SendMessage();
				Task.Delay(5000);
			}
		});
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
	}

	private void InitializeDevices(bool onlyNew = true)
	{
		List<Device> devices = onlyNew ? Devices.Where(d => d.State == DeviceState.None).ToList() : Devices;
		foreach (Device device in devices)
		{
			device.State = DeviceState.Loaded;
			ErrorCode errorCode = device.Initialize();
			if (errorCode > 0)
			{
				ShowMessageBox(errorCode, device.Name);
				continue;
			}

			device.State = DeviceState.Initialized;
			device.StateColor = new SolidColorBrush(Color.FromRgb(0, 128, 0));
		}
	}

	private void RetrieveFromDevices()
	{
		foreach (Device device in workingDevices)
		{
			device.Retrieve();
			device.ConvertValueToStandard();
		}
	}

	private void SendMessage()
	{
		foreach (Device device in workingDevices)
		{
			device.Send(device.StandardizedValue);
		}
	}

	private void FindWorkingDevices()
	{
		workingDevices = Devices.Where(d => d.State == DeviceState.Working).ToList();
	}

	private delegate void SendMessageDelegate(string message);

	private async void SendDataByPipe_OnChecked(object sender, RoutedEventArgs e)
	{
		Device device = GetDeviceByInputName<RadioButton>(sender);
		ErrorCode code = await device.CreatePipe();
		if (code != ErrorCode.Success)
			ShowMessageBox(code);
		else
			device.SendData = SendDataMode.Pipe;
	}

	private void SendDataByUdp_OnChecked(object sender, RoutedEventArgs e)
	{
		/*if (global.IsRunVRTherapy)
			MessageBox.Show("Aplikacja odbierająca to VRTherapy. W takim przypadku zalecane jest użycie potoku.");*/
		Device device = GetDeviceByInputName<RadioButton>(sender);
		ErrorCode code = device.CreateSocket("Send");
		if (code != ErrorCode.Success)
			ShowMessageBox(code);
		else
			device.SendData = SendDataMode.Network;
	}

	private void SendDataNone_OnChecked(object sender, RoutedEventArgs e)
	{
		sendMessage = null;
	}

	private void SignalsComboBoxItemChanged(object sender, SelectionChangedEventArgs e)
	{
		Device device = GetDeviceByInputName<ComboBox>(sender);
		ComboBox comboBox = sender as ComboBox;
		string signal = comboBox.SelectedItem.ToString();
		device.AddSignalChosen(signal);
		RefreshChannelValues();
	}

	private void RefreshChannelValues()
	{
		DevicesListBox.Dispatcher.BeginInvoke((Action)(() =>
		{
			foreach (var device in Devices)
			{
				for (int j = 0; j < device.SignalsChosen.Count; j++)
				{
					string signal = device.SignalsChosen[j];
					WrapPanel wrapPanel = (DevicesListBox.Children[j + 1] as GroupBox).Content as WrapPanel;
					TextBlock channelValue = Util.FindChild<TextBlock>(wrapPanel, $"ChannelValue{j}");
					var values = device.Signals[signal].Values;
					channelValue.Text = values.Count > 0 ? device.Signals[signal].Values.Last().ToString() : "0.0";
					double value = double.Parse(channelValue.Text);
					UpdateChannelValueIndicator(wrapPanel, value, j);
				}
			}
		}));
	}

	private void UpdateChannelValueIndicator(WrapPanel wrapPanel, double value, int number)
	{
		double min = double.Parse(Util.FindChild<TextBox>(wrapPanel, $"MinRangeValue{number}").Text);
		double max = double.Parse(Util.FindChild<TextBox>(wrapPanel, $"MaxRangeValue{number}").Text);
		TextBlock channelValueIndicator = Util.FindChild<TextBlock>(wrapPanel, $"ChannelValueIndicator{number}");
		if (value < min || value > max)
		{
			channelValueIndicator.Text = value < min ? "\u2b0a" : "\u2b08";
			channelValueIndicator.Foreground = new SolidColorBrush(Color.FromRgb(205, 0, 50));
		}

		if (value > min && value < max)
		{
			channelValueIndicator.Text = "✔";
			channelValueIndicator.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 0));
		}
	}

	private void RetrieveDataByDriver_OnChecked(object sender, RoutedEventArgs e)
	{
		Device device = GetDeviceByInputName<RadioButton>(sender);
		device.RetrieveData = RetrieveDataMode.Driver;
	}

	private void RetrieveDataByUdp_OnChecked(object sender, RoutedEventArgs e)
	{
		Device device = GetDeviceByInputName<RadioButton>(sender);
		ErrorCode code = device.CreateSocket("Retrieve");
		if (code != ErrorCode.Success)
			ShowMessageBox(code);
		else
			device.RetrieveData = RetrieveDataMode.Network;
	}

	private void RetrieveDataNone_OnChecked(object sender, RoutedEventArgs e)
	{
	}

	private void RetrieveDataByUdpPort_OnTextChanged(object sender, TextChangedEventArgs e)
	{
		if (Devices.Count == 0) return;
		TextBox tb = sender as TextBox;
		Device device = GetDeviceByInputName<TextBox>(sender);
		device.retrievePort = int.Parse(tb.Text);
		Console.WriteLine(device.retrievePort);
	}

	private void SendDataByUdpPort_OnTextChanged(object sender, TextChangedEventArgs e)
	{
		if (Devices.Count == 0) return;
		TextBox tb = sender as TextBox;
		Device device = GetDeviceByInputName<TextBox>(sender);
		device.sendPort = int.Parse(tb.Text);
	}

	private Device GetDeviceByInputName<T>(object sender) where T : Control
	{
		T input = sender as T;
		return Devices.Find(d => input.Name.Contains(d.Code));
	}

	public Control GetInputByDeviceName<T>(string inputType, string deviceName) where T : Control
	{
		return Util.FindChild<T>(MainGrid, inputType + "_" + deviceName);
	}
}