using System.IO;
using System.IO.Pipes;

namespace SensorsInterface;

public class Global
{
	public bool IsRunVRTherapy = false;
	public static Global? Instance;
	private static NamedPipeServerStream simulatorPipe;
	public StreamWriter SimulatorPipeWriter;
	//private IPEndPoint endPoint = new(IPAddress., 8085);
	
	public static MainWindow window;
	public Global()
	{
		if (Instance != null)
			return;
		Instance = this;
		/*Task.Run(() =>
		{
			Process[] pname;
			while (true)
			{
				pname = Process.GetProcessesByName("TherapyDesktopV2");
				IsRunVRTherapy = pname.Length > 0;
				Task.Delay(5000);
			}
		});*/
		//Task.Run(() =>
		{
			/*while (true)
			{
				if (simulatorPipe == null)
				{
					simulatorPipe = new NamedPipeServerStream("SimulatorPipe");
					SimulatorPipeWriter = new StreamWriter(simulatorPipe);
					simulatorPipe.WaitForConnection();
					break;
				}

				Task.Delay(5000);
			}*/
		}//);
	}
}