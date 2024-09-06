using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SensorsInterface.Devices;
using SensorsInterface.Helpers;
using static SensorsInterface.Devices.Device;

namespace SensorsInterface;

public partial class MainWindow
{
	public readonly Dictionary<DeviceState, SolidColorBrush> StateColors = new()
	{
		{ DeviceState.None, new SolidColorBrush(Color.FromRgb(255, 0, 255)) },
		{ DeviceState.Loaded, new SolidColorBrush(Color.FromRgb(128, 128, 128)) },
		{ DeviceState.Initialized, new SolidColorBrush(Color.FromRgb(106, 128, 0)) },
		{ DeviceState.Working, new SolidColorBrush(Color.FromRgb(0, 128, 0)) },
		{ DeviceState.Stopped, new SolidColorBrush(Color.FromRgb(255, 128, 0)) },
		{ DeviceState.Error, new SolidColorBrush(Color.FromRgb(255, 0, 75)) },
	};

	private const double ellipseSize = 18;
	private const double deviceHeight = 360;
	private const double elementMargin = 15;
	private const double elementMarginThin = 5;
	private const double elementFlatMarginTop = 14;
	private const double windowMargin = 10;

	private WrapPanel CreateButtons(string deviceCode)
	{
		WrapPanel panel = new WrapPanel
		{
			Children =
			{
				new GroupBox
				{
					Header = "Pobieranie danych",
					Width = 380,
					Margin = new Thickness(windowMargin, 5, 0, 10),
					Content = new WrapPanel
					{
						Children =
						{
							new RadioButton
							{
								Name = $"{deviceCode}RetrieveDataByDriver",
								GroupName = "RetrieveMode",
								Content = "Pobierz sterownikiem"
							},
							new RadioButton
							{
								Name = $"{deviceCode}RetrieveDataByUdp",
								GroupName = "RetrieveMode",
								Content = "Pobierz sieciowo"
							},
							new TextBox
							{
								Name = $"{deviceCode}RetrieveDataByUdpPort",
								Text = "8053"
							},
							new RadioButton
							{
								Name = $"{deviceCode}RetrieveDataNone",
								GroupName = "RetrieveMode",
								Content = "Nie pobieraj",
								IsChecked = true
							}
						}
					}
				},
				new GroupBox
				{
					Header = "Wysyłanie danych",
					Width = 380,
					Margin = new Thickness(windowMargin, 5, 0, 10),
					Content = new WrapPanel
					{
						Children =
						{
							new RadioButton
							{
								Name = $"{deviceCode}SendDataByPipe",
								GroupName = "SendMode",
								Content = "Wyślij potokiem"
							},
							new RadioButton
							{
								Name = $"{deviceCode}SendDataByUdp",
								GroupName = "SendMode",
								Content = "Wyślij sieciowo"
							},
							new TextBox
							{
								Name = $"{deviceCode}SendDataNone",
								Text = "8054"
							},
							new RadioButton
							{
								Name = $"{deviceCode}SendDataNone",
								GroupName = "SendMode",
								Content = "Nie wysyłaj",
								IsChecked = true
							}
						}
					}
				}
			}
		};

		//Retrieve
		UIElementCollection children = ((panel.Children[0] as GroupBox).Content as WrapPanel).Children;
		(children[0] as RadioButton).Checked += RetrieveDataByDriver_OnChecked;
		(children[1] as RadioButton).Checked += RetrieveDataByUdp_OnChecked;
		(children[2] as TextBox).TextChanged += RetrieveDataByUdpPort_OnTextChanged;
		(children[3] as RadioButton).Checked += RetrieveDataNone_OnChecked;

		//Send
		children = ((panel.Children[1] as GroupBox).Content as WrapPanel).Children;
		(children[0] as RadioButton).Checked += SendDataByPipe_OnChecked;
		(children[1] as RadioButton).Checked += SendDataByUdp_OnChecked;
		(children[2] as TextBox).TextChanged += SendDataByUdpPort_OnTextChanged;
		(children[3] as RadioButton).Checked += SendDataNone_OnChecked;

		return panel;
	}

	private void CreateHUD()
	{
		int i = 0;
		foreach (Device device in hiddenDevices)
		{
			Button button = new Button
			{
				Content = "Rozpocznij pomiary",
				Width = 130
			};

			button.Click += (_, _) =>
			{
				switch (device.State)
				{
					case DeviceState.Initialized:
						/*if (GetInputByDeviceName<RadioButton>("RetrieveDataNone", device.Code).IsChecked == true)
						                        ShowMessageBox(ErrorCode.RetrieveDataModeNotSelected);*/

						/*if ((GetInputByDeviceName<RadioButton>("SendDataNone", device.Code)).IsChecked == true)
							ShowMessageBox(ErrorCode.SendDataModeNotSelected);*/
						device.State = DeviceState.Working;
						button.Content = "Zatrzymaj pomiary";
						break;
					case DeviceState.Working:
						device.State = DeviceState.Initialized;
						button.Content = "Rozpocznij pomiary";
						break;
				}
			};
			Canvas.SetLeft(button, 630);

			StackPanel panel = new()
			{
				Name = $"{device.Code}Panel",
				Visibility = Visibility.Collapsed,
				Orientation = Orientation.Vertical,
				//Margin = new Thickness(windowMargin, i++*deviceHeight, 0, 0),
				Children =
				{
					CreateButtons(device.Code),
					//Device Name
					new WrapPanel
					{
						Orientation = Orientation.Horizontal,
						Name = $"{device.Code}StackPanel",
						//Margin = new Thickness(10, i++ * deviceHeight, 0, 0),
						Margin = new Thickness(2 * windowMargin, 0, 0, 2 * windowMargin),
						Children =
						{
							new Canvas
							{
								Children =
								{
									new Ellipse
									{
										Fill = device.StateColor,
										Width = ellipseSize,
										Height = ellipseSize
									},
									new TextBlock
									{
										Text = device.Name,
										Margin = new Thickness(ellipseSize + 5, 0, 0, 0)
									},
									button
								}
							}
						}
					},
					CreateHUDDevice(device)
				}
			};
			//Canvas.SetLeft(tb, ellipseSize);
			MainGrid.Children.Add(panel);
		}
		MainGrid.Children.Add(new StackPanel
		{
			Name = "NotFoundDevices",
			Children =
			{
				new TextBlock
				{
					Text = "⚠ Nie podłączono żadnego urządzenia ⚠",
					Margin = new Thickness(0, 150, 50, 0),
					FontSize = 15,
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center
				}
			}
		});
	}

	private WrapPanel CreateHUDDevice(Device device)
	{
		WrapPanel channelsWrapPanel = new()
		{
			Orientation = Orientation.Vertical,
			Margin = new Thickness(windowMargin, 0, 0, 0)
		};
		for (int i = 0; i < device.ChannelsNumber; i++)
		{
			CheckBox channelEnable = new CheckBox
			{
				Name = $"ChannelEnable{i}",
				IsChecked = true,
				Width = 20,
				Height = 20,
				Margin = new Thickness(0, elementFlatMarginTop, elementMargin, 0)
			};
			int i1 = i;
			channelEnable.Click += (_, _) => { device.SetChannelState(i1, channelEnable.IsChecked == true); };

			//Signals
			ComboBox signalsComboBox = new ComboBox
			{
				Name = $"{device.Code}Channel{i}",
				Width = 80,
				//Margin = new Thickness(0, 0, elementMargin, 0),
				ItemsSource = device.Signals.Keys,
				SelectedItem = device.SignalsChosen.FindKeyByIndex(i)
			};
			signalsComboBox.SelectionChanged += SignalsComboBoxItemChanged;

			//ChannelFunction
			ComboBox channelFunctionComboBox = new ComboBox
			{
				Name = $"{device.Code}ChannelFunction{i}",
				Width = 100,
				//Margin = new Thickness(0, 0, elementMargin, 0),
				ItemsSource = device.ChannelFunctionsPolish.Values,
				SelectedItem = device.ChannelFunctionsPolish["Voltage"]
			};
			channelFunctionComboBox.SelectionChanged += ChannelFunctionComboBoxItemChanged;

			//Częstotliwość
			ComboBox frequencyComboBox = new ComboBox
			{
				Name = $"{device.Code}Frequency{i}",
				Width = 80,
				//Margin = new Thickness(0, 0, elementMargin, 0),
				ItemsSource = device.Frequencies,
				SelectedItem = device.Frequencies[0]
			};
			frequencyComboBox.SelectionChanged += FrequencyComboBoxItemChanged;

			GroupBox groupBox = new GroupBox
			{
				Header = "Kanał " + (i + 1),
				Name = $"{device.Code}Channel{i}",
				Width = 800,
				Height = 70,
				//Margin = new Thickness(0, 30, 0, 0),
				Content = new WrapPanel
				{
					Orientation = Orientation.Horizontal,
					Name = $"{device.Code}Signals",
					Children =
					{
						new WrapPanel
						{
							Children =
							{
								new TextBlock
								{
									Text = "Włącz",
									Margin = new Thickness(10, elementFlatMarginTop - 2, elementMarginThin, 0),
								},
								channelEnable,
							}
						},

						//SignalState
						new WrapPanel
						{
							Orientation = Orientation.Vertical,
							Margin = new Thickness(0, 0, elementMargin, 0),
							Children =
							{
								new Label
								{
									Content = "Stan",
									Margin = new Thickness(0, -10, 0, 0)
								},
								new Ellipse
								{
									Name = $"{device.Code}SignalState{i}",
									Fill = StateColors[DeviceState.Initialized],
									Width = ellipseSize,
									Height = ellipseSize
								},
							}
						},
						//Signals
						new WrapPanel
						{
							Orientation = Orientation.Vertical,
							Margin = new Thickness(0, 0, elementMargin, 0),
							Children =
							{
								new Label
								{
									Content = "Typ sygnału",
									Margin = new Thickness(0, -10, 0, 0)
								},
								signalsComboBox,
							}
						},
						new TextBlock
						{
							Name = $"ChannelValue{i}",
							Text = "——",
							Width = 60,
							FontSize = 16,
							FontWeight = FontWeights.SemiBold,
							TextAlignment = TextAlignment.Center,
							Margin = new Thickness(0, elementFlatMarginTop, 0, 0)
						},
						new TextBlock
						{
							Name = $"ChannelUnit{i}",
							Text = " V",
							Width = 15,
							FontSize = 16,
							FontWeight = FontWeights.SemiBold,
							TextAlignment = TextAlignment.Left,
							Margin = new Thickness(0, elementFlatMarginTop, elementMargin, 0)
						},
						//Range
						new WrapPanel
						{
							Name = $"RangeLabelWrapPanel{i}",
							Orientation = Orientation.Vertical,
							Margin = new Thickness(0, 0, elementMargin, 0),
							Children =
							{
								new Label
								{
									Content = "Zakres",
									Margin = new Thickness(4 * elementMargin, -8, 2 * elementMargin, 0),
								},
								new WrapPanel
								{
									Name = $"RangeWrapPanel{i}",
									Children =
									{
										new TextBlock
										{
											Name = $"ChannelValueIndicator{i}",
											Margin = new Thickness(0, 0, elementMarginThin, 0),
											Text = "❔",
											Width = 15,
											Foreground = new SolidColorBrush(Color.FromRgb(255, 190, 0))
										},
										new TextBlock
										{
											Text = "Min",
											Margin = new Thickness(0, 0, elementMarginThin, 0),
										},
										new TextBox
										{
											Name = $"MinRangeValue{i}",
											Text = "50",
											Width = 45,
											Margin = new Thickness(0, 0, elementMarginThin, 0),
											TextAlignment = TextAlignment.Center
										},
										new TextBlock
										{
											Text = "Max",
											Margin = new Thickness(0, 0, elementMarginThin, 0),
										},
										new TextBox
										{
											Name = $"MaxRangeValue{i}",
											Text = "80",
                                            Width = 45,
                                            TextAlignment = TextAlignment.Center
										}
									}
								},
							}
						},
						//Channel Function
						new WrapPanel
						{
							Name = $"ChannelFunctionWrapPanel{i}",
							Orientation = Orientation.Vertical,
							Margin = new Thickness(0, 0, elementMargin, 0),
							Children =
							{
								new Label
								{
									Content = "Funkcja kanału",
									Margin = new Thickness(0, -10, 0, 0)
								},
								channelFunctionComboBox
							}
						},
						//Frequency
						new WrapPanel
						{
							Name = $"{device.Code}FrequencyWrapPanel{i}",
							Orientation = Orientation.Vertical,
							Margin = new Thickness(0, 0, elementMargin, 0),
							Children =
							{
								new Label
								{
									Content = "Częstotliwość",
									Margin = new Thickness(0, -10, 0, 0)
								},
								frequencyComboBox
							}
						}
					}
				}
			};
			channelsWrapPanel.Children.Add(groupBox);
		}

		return channelsWrapPanel;
	}

	private void RefreshHUD()
	{
		Dispatcher.BeginInvoke((Action)delegate
		{
			if (devices.Count == 0)
			{
				foreach (UIElement panel in MainGrid.Children)
					panel.Visibility = Visibility.Collapsed;
				MainGrid.Children[^1].Visibility = Visibility.Visible;
			}
			else
			{
				foreach (Device device in devices)
				{
					StackPanel stackPanel =
						MainGrid.Children.OfType<StackPanel>().First(p => p.Name == $"{device.Code}Panel");
					stackPanel.Visibility = Visibility.Visible;
				}
				MainGrid.Children[^1].Visibility = Visibility.Collapsed;
			}
		});
	}
}