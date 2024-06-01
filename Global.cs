using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using SensorsInterface.Helpers;

namespace SensorsInterface;

public class Global
{
	public bool IsRunVRTherapy = false;
	public static Global? Instance;
	private static NamedPipeServerStream simulatorPipe;
	public static StreamWriter SimulatorPipeWriter;

	public Global()
	{
		if (Instance != null)
			return;
		Instance = this;
		Task.Run(() =>
		{
			Process[] pname;
			while (true)
			{
				pname = Process.GetProcessesByName("TherapyDesktopV2");
				IsRunVRTherapy = pname.Length > 0;
				Task.Delay(5000);
			}
		});
		Task.Run(() =>
		{
			if (simulatorPipe == null)
			{
				simulatorPipe = new NamedPipeServerStream("SimulatorPipe");
				SimulatorPipeWriter = new StreamWriter(simulatorPipe);
			}
			simulatorPipe.WaitForConnectionEx();
			Task.Delay(5000);
		});
}

}