using System.Runtime.InteropServices;

namespace SensorsInterface.Native;

public static class NativeMethods
{
	[DllImport("kernel32.dll")]
	public static extern IntPtr LoadLibrary(string dllToLoad);

	[DllImport("kernel32.dll")]
	public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

	[DllImport("kernel32.dll")]
	public static extern bool FreeLibrary(IntPtr hModule);
	
	[DllImport("kernel32.dll")]
	public static extern int GetLastError();
}