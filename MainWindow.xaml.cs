using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SensorsInterface.Devices;
using SensorsInterface.Devices.NeurobitOptima;
using SensorsInterface.Devices.Simulator;
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
	private List<Device> devices = [];
	private List<Device> workingDevices = [];
	private List<Device> lastAddedDevices = [];

	private List<Device> hiddenDevices =
	[
		new NeurobitOptima(),
		new DeviceSimulator()
	];

	private List<string> connectedApplications = [];

	public MainWindow()
	{
		CultureInfo ci = new CultureInfo("en-US");
		Thread.CurrentThread.CurrentCulture = ci;
		Thread.CurrentThread.CurrentUICulture = ci;
		InitializeComponent();
		Global.window = this;

		CreateHUD();
		//devices.Add(hiddenDevices[0]);
		Task.Run(() =>
		{
			//Thread.Sleep(1000);
			devices.Add(hiddenDevices[1]);
		});

		Closing += AddDeviceClosing;

		Task.Run(() =>
		{
			while (true)
			{
				GetConnectedDevices();
				InitializeDevices();
				RefreshHUD();
				FindWorkingDevices();
				RetrieveFromDevices();
				RefreshChannelValues();
				SendMessage();
				CheckDevicesConnection();
				CheckApplicationsConnection();
				lastAddedDevices.Clear();
			}
		});
	}

	private void AddDeviceClosing(object? sender, CancelEventArgs cancelEventArgs)
	{
		foreach (Device device in devices)
			device.Close();
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
			if (device != null && !devices.Contains(device))
			{
				devices.Add(device);
				lastAddedDevices.Add(device);
			}
		}
	}

	private void InitializeDevices(bool onlyNew = true)
	{
		List<Device> devices = onlyNew ? this.devices.Where(d => d.State == DeviceState.None).ToList() : this.devices;
		foreach (Device device in devices)
		{
			device.State = DeviceState.Loaded;
			ErrorCode errorCode = device.Initialize();
			if (errorCode > 0)
			{
				ShowMessageBox(errorCode, device.Name);
				devices.Remove(device);
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
			device.Send(device.StandardizedValue);
	}

	private void CheckDevicesConnection()
	{
		foreach (Device device in devices.ToList())
		{
			if (device.CheckDeviceState() == ErrorCode.DeviceIsDisconnected)
			{
				ShowMessageBox(ErrorCode.DeviceIsDisconnected, device.Name);
				Dispatcher.BeginInvoke((Action)delegate
				{
					StackPanel stackPanel =
						MainGrid.Children.OfType<StackPanel>().First(p => p.Name == $"{device.Code}Panel");
					stackPanel.Visibility = Visibility.Collapsed;
				});
				devices.Remove(device);
			}
		}
	}

	private void CheckApplicationsConnection()
	{
		Process[] processes = Process.GetProcesses();
		foreach (string application in connectedApplications.ToList())
		{
			if (processes.FirstOrDefault(p => p.ProcessName == application, null) == null)
			{
				ShowMessageBox(ErrorCode.ApplicationIsDisconnected, application);
				connectedApplications.Remove(application);
			}
		}
	}

	private void FindWorkingDevices()
	{
		workingDevices = devices.Where(d => d.State == DeviceState.Working).ToList();
	}

	private async void SendDataByPipe_OnChecked(object sender, RoutedEventArgs e)
	{
		Application.Current.MainWindow.Title = "Sensors Interface (pipe)";
		Device device = GetDeviceByInputName<RadioButton>(sender);
		ErrorCode code = await device.CreatePipe();
		if (code != ErrorCode.Success)
		{
			ShowMessageBox(code);
			Application.Current.MainWindow.Title = "Sensors Interface";
		}
		else
		{
			device.SendData = SendDataMode.Pipe;
			connectedApplications.Add("TherapyDesktopV2");
		}
	}

	private void SendDataByUdp_OnChecked(object sender, RoutedEventArgs e)
	{
		if (Global.IsRunVRTherapy)
			MessageBox.Show("Aplikacja odbierająca to VRTherapy. W takim przypadku zalecane jest użycie potoku.",
				"Zalecany potok", MessageBoxButton.OK, MessageBoxImage.Information);
		Device device = GetDeviceByInputName<RadioButton>(sender);
		ErrorCode code = device.CreateSocket("Send");
		if (code != ErrorCode.Success)
			ShowMessageBox(code);
		else
		{
			device.SendData = SendDataMode.Network;
			Application.Current.MainWindow.Title = "Sensors Interface (network)";
			connectedApplications.Add("TherapyDesktopV2");
		}
	}

	private void SendDataNone_OnChecked(object sender, RoutedEventArgs e)
	{
		Device device = GetDeviceByInputName<RadioButton>(sender);
		device.SendData = SendDataMode.None;
	}

	private void SignalsComboBoxItemChanged(object sender, SelectionChangedEventArgs e)
	{
		Device device = GetDeviceByInputName<ComboBox>(sender);
		ComboBox comboBox = sender as ComboBox;
		string signal = comboBox.SelectedItem.ToString();
		string result = device.AddSignalChosen(signal, comboBox.SelectedIndex, int.Parse(comboBox.Name[^1].ToString()));
		if (result != "")
		{
			comboBox.SelectionChanged -= SignalsComboBoxItemChanged;
			comboBox.SelectedItem = result;
			comboBox.SelectionChanged += SignalsComboBoxItemChanged;
		}
	}

	private void FrequencyComboBoxItemChanged(object sender, SelectionChangedEventArgs e)
	{
		Device device = GetDeviceByInputName<ComboBox>(sender);
		ComboBox comboBox = sender as ComboBox;
		double frequency = (double)comboBox.SelectedItem;
		string signal = Util.FindChild<ComboBox>(MainGrid, $"{device.Code}Channel{comboBox.Name[^1]}")
			.SelectedItem.ToString();
		device.SetFrequency(signal, frequency);
	}

	private void ChannelFunctionComboBoxItemChanged(object sender, SelectionChangedEventArgs e)
	{
		Device device = GetDeviceByInputName<ComboBox>(sender);
		ComboBox comboBox = sender as ComboBox;
		string channelFunction = comboBox.SelectedItem.ToString();
		string translated = device.ChannelFunctionsPolish.First(v => v.Value == channelFunction).Key;
		Util.FindChild<TextBlock>(MainGrid, $"ChannelUnit{comboBox.Name[^1]}").Text =
			device.ChannelFunctionsUnits[translated];
		device.ChannelFunctionsChosen[int.Parse(comboBox.Name[^1].ToString())] = translated;

		GroupBox channelGroup = Util.FindChild<GroupBox>(MainGrid, $"{device.Code}Channel{comboBox.Name[^1]}");
		WrapPanel frequencyWrapPanel = (channelGroup.Content as WrapPanel).Children[7] as WrapPanel;
		frequencyWrapPanel.Visibility = translated == "Voltage" ? Visibility.Visible : Visibility.Hidden;
	}

	private void RefreshChannelValues()
	{
		Dispatcher.BeginInvoke((Action)delegate
		{
			int i = -1;
			foreach (var device in hiddenDevices)
			{
				i++;
				if (!workingDevices.Contains(device)) continue;
				UIElementCollection channels = ((MainGrid.Children[i] as StackPanel).Children[2] as WrapPanel).Children;
				for (int j = 0; j < device.SignalsChosen.Count; j++)
				{
					WrapPanel channel = (channels[j] as GroupBox).Content as WrapPanel;
					int j1 = j;
					string signal = device.SignalsChosen.FindValueByIndex(j1).Name;
					TextBlock channelValue = channel.Children[3] as TextBlock;
					var values = device.Signals[signal].Values;
					channelValue.Text = values.Count > 0 ? Format(device.Signals[signal].Values.Last().Value) : "0.0";
					double value = double.Parse(channelValue.Text);
					WrapPanel rangeWrapPanel = (channel.Children[5] as WrapPanel).Children[1] as WrapPanel;
					UpdateChannelValueIndicator(rangeWrapPanel, device, value, j1);
				}
			}
		});
	}

	public static string Format(double number)
	{
		return $"{Math.Round(number, 4):f}";
	}

	private void UpdateChannelValueIndicator(WrapPanel wrapPanel, Device device, double value, int number)
	{
		double min = double.Parse(Util.FindChild<TextBox>(wrapPanel, $"MinRangeValue{number}").Text);
		double max = double.Parse(Util.FindChild<TextBox>(wrapPanel, $"MaxRangeValue{number}").Text);
		TextBlock channelValueIndicator = Util.FindChild<TextBlock>(wrapPanel, $"ChannelValueIndicator{number}");
		if (value < min || value > max)
		{
			channelValueIndicator.Text = value < min ? "\u2b0a" : "\u2b08";
			channelValueIndicator.Foreground = new SolidColorBrush(Color.FromRgb(205, 0, 50));
			device.RangeStates[number] = value < min ? RangeState.Low : RangeState.High;
		}

		if (value > min && value < max)
		{
			channelValueIndicator.Text = "✔";
			channelValueIndicator.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 0));
			device.RangeStates[number] = RangeState.Normal;
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
		Device device = GetDeviceByInputName<RadioButton>(sender);
		device.RetrieveData = RetrieveDataMode.None;
	}

	private void RetrieveDataByUdpPort_OnTextChanged(object sender, TextChangedEventArgs e)
	{
		if (devices.Count == 0) return;
		TextBox tb = sender as TextBox;
		Device device = GetDeviceByInputName<TextBox>(sender);
		device.retrievePort = int.Parse(tb.Text);
	}

	private void SendDataByUdpPort_OnTextChanged(object sender, TextChangedEventArgs e)
	{
		if (devices.Count == 0) return;
		TextBox tb = sender as TextBox;
		Device device = GetDeviceByInputName<TextBox>(sender);
		device.sendPort = int.Parse(tb.Text);
	}

	private Device GetDeviceByInputName<T>(object sender) where T : Control
	{
		T input = sender as T;
		return devices.Find(d => input.Name.Contains(d.Code));
	}

	public T GetInputByDeviceName<T>(string inputType, string deviceName) where T : Control
	{
		return Util.FindChild<T>(MainGrid, deviceName + inputType);
	}
}