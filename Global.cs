using System.Diagnostics;

namespace SensorsInterface;

public static class Global
{
	public static bool IsRunVRTherapy;
	
	public static MainWindow window;
	static Global()
	{
		Task.Run(() =>
		{
			Process[] pname;
			while (true)
			{
				pname = Process.GetProcessesByName("TherapyDesktopV2");
				if (pname.Length > 0)
				{
					IsRunVRTherapy = true;
					return;
				}
				Task.Delay(5000);
			}
		});
	}
}