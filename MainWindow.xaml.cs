using System.Windows;
using SensorsInterface.Devices;

namespace SensorsInterface;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
	private List<Device> Devices = [];
	public MainWindow()
	{
		InitializeComponent();
		Devices.Add(new NeurobitOptima());
	}
}
