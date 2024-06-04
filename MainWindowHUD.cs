using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SensorsInterface.Devices;
using SensorsInterface.Devices.NeurobitOptima;
using static SensorsInterface.Devices.Device;
using static SensorsInterface.Helpers.Error;

namespace SensorsInterface;

public partial class MainWindow
{
	private const double ellipseSize = 18;
	private const double deviceHeight = 75;

	private void CreateHUD()
	{
		/*Task.Run(() =>
		{

			while (true)
			{*/
				if (devices.Count == 0)
				{
					TextBlock tb = new TextBlock
					{
						Text = "Nie podłączono żadnego urządzenia"
					};
					DevicesListBox.Children.Add(tb);
					return;
				}

				int i = 0;
				foreach (Device device in devices)
				{
					StackPanel sp = new()
					{
						Orientation = Orientation.Horizontal,
						Margin = new Thickness(0, i++ * deviceHeight, 0, 0)
					};
					Canvas c = new Canvas();

					Ellipse e = new()
					{
						Fill = device.StateColor,
						Width = ellipseSize,
						Height = ellipseSize
					};

					TextBlock tb = new()
					{
						Text = device.Name
					};
					Canvas.SetLeft(tb, ellipseSize);

					Button b = new Button
					{
						Content = "Start"
					};

					b.Click += (_, _) =>
					{
						if ((GetInputByDeviceName<RadioButton>("RetrieveDataNone", device.Code) as RadioButton)
						    .IsChecked ==
						    true)
						{
							ShowMessageBox(ErrorCode.RetrieveDataModeNotSelected);
							return;
						}

						/*if ((GetInputByDeviceName<RadioButton>("SendDataNone", device.Code) as RadioButton).IsChecked == true)
							ShowMessageBox(ErrorCode.SendDataModeNotSelected);*/
						device.State = DeviceState.Working;
					};
					Canvas.SetLeft(b, 700);
					c.Children.Add(e);
					c.Children.Add(tb);
					c.Children.Add(b);
					sp.Children.Add(c);
					DevicesListBox.Children.Add(sp);
				}

				if (lastAddedDevices.FirstOrDefault(d => d is NeurobitOptima, null) != null)
					CreateHUDNeurobitOptima((NeurobitOptima)lastAddedDevices.Find(d => d is NeurobitOptima));
			/*}
		});*/


	}

	private void CreateHUDNeurobitOptima(NeurobitOptima device)
	{
		for (int i = 0; i < device.ChannelsNumber; i++)
		{
			WrapPanel wrapPanel = new WrapPanel
			{
				Orientation = Orientation.Horizontal
			};

			GroupBox groupBox = new GroupBox
			{
				Header = "Kanał "+(i+1),
				Width = 800
			};

			CheckBox channelEnable = new CheckBox
			{
				Name = $"ChannelEnable{i}",
				IsChecked = true,
				Width = 20,
				Height = 20,
				Margin = new Thickness(0,0,6,0)
			};
			int i1 = i;
			channelEnable.Click += (_,_) =>
			{
				device.SetChannelState(i1, channelEnable.IsChecked==true);
			};

			//Signals
			WrapPanel signalsWrapPanel = new WrapPanel
			{
				Orientation = Orientation.Vertical
			};
			
			Label signalsLabel = new Label
			{
				Content = "Typ sygnału",
				Margin = new Thickness(0,-10,0,0)
			};
			
			ComboBox signalsComboBox = new ComboBox
			{
				Name = $"NeurobitOptimaChannel{i}",
				ItemsSource = device.SignalsAvailable,
				Width = 80,
				Margin = new Thickness(0,0,6,0)
			};
			signalsComboBox.SelectionChanged += SignalsComboBoxItemChanged;
			
			signalsWrapPanel.Children.Add(signalsLabel);
			signalsWrapPanel.Children.Add(signalsComboBox);
			TextBlock signalValue = new TextBlock
			{
				Name = $"ChannelValue{i}",
				Text = "0.0",
				Width = 50,
				FontSize = 16,
				FontWeight = FontWeights.SemiBold,
				TextAlignment = TextAlignment.Center,
				Margin = new Thickness(0,15,0,0)
			};

			//Zakres
			Label rangeLabel = new Label
			{
				Content = "Zakres",
				Margin = new Thickness(0,-10,0,0)
			};
			
			/*GroupBox rangeGroupBox = new GroupBox
			{
				Header = "Zakres",
				Margin = new Thickness(-5,-5,-5,-5),
				BorderThickness = new Thickness(0)
			};*/

			WrapPanel rangeWrapPanel0 = new WrapPanel
			{
				Name = $"RangeWrapPanel{i}0",
				Orientation = Orientation.Vertical
			};
			
			WrapPanel rangeWrapPanel = new WrapPanel
			{
				Name = $"RangeWrapPanel{i}"
			};
			rangeWrapPanel0.Children.Add(rangeLabel);
			rangeWrapPanel0.Children.Add(rangeWrapPanel);
			TextBlock signalValueIndicator = new TextBlock
			{
				Name = $"ChannelValueIndicator{i}",
				Text = "❔",
				Foreground = new SolidColorBrush(Color.FromRgb(255,190,0))
			};

			TextBlock minRange = new TextBlock
			{
				Text = "Min"
			};

			TextBox minRangeValue = new TextBox
			{
				Name = $"MinRangeValue{i}",
				Text = "50"
			};
			
			TextBlock maxRange = new TextBlock
			{
				Text = "Max"
			};
			
			TextBox maxRangeValue = new TextBox
			{
				Name = $"MaxRangeValue{i}",
				Text = "80"
			};

			//rangeGroupBox.Content = rangeWrapPanel;
			//rangeWrapPanel.Children.Add(rangeLabel);
			rangeWrapPanel.Children.Add(signalValueIndicator);
			rangeWrapPanel.Children.Add(minRange);
			rangeWrapPanel.Children.Add(minRangeValue);
			rangeWrapPanel.Children.Add(maxRange);
			rangeWrapPanel.Children.Add(maxRangeValue);
			
			//Częstotliwość
			WrapPanel frequencyWrapPanel = new WrapPanel
			{
				Name = $"FrequencyWrapPanel{i}",
				Orientation = Orientation.Vertical
			};

			Label frequencyLabel = new Label
			{
				Content = "Częstotliwość"
			};
			
			ComboBox frequencyComboBox = new ComboBox
			{
				Name = $"NeurobitOptimaFrequency{i}",
				ItemsSource = device.Frequencies,
				Width = 80,
				Margin = new Thickness(0,0,6,0)
			};
			frequencyComboBox.SelectionChanged += FrequencyComboBoxItemChanged;
			
			frequencyWrapPanel.Children.Add(frequencyLabel);
			frequencyWrapPanel.Children.Add(frequencyComboBox);
			
			//Reszta
			groupBox.Content = wrapPanel;
			wrapPanel.Children.Add(channelEnable);
			//wrapPanel.Children.Add(label);
			wrapPanel.Children.Add(signalsWrapPanel);
			wrapPanel.Children.Add(signalValue);
			//wrapPanel.Children.Add(rangeGroupBox);
			wrapPanel.Children.Add(rangeWrapPanel0);
			wrapPanel.Children.Add(frequencyWrapPanel);
			groupBox.Margin = new Thickness
			{
				Top = 30
			};
			DevicesListBox.Children.Add(groupBox);
		}
	}
}