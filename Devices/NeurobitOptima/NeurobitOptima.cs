using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using static SensorsInterface.NativeMethods;

namespace SensorsInterface.Devices;

public unsafe partial class NeurobitOptima : Device
{
	public override List<string> SignalsNames { get; set; } = ["EKG", "RestTemp"];
	public override string DeviceName { get; set; } = "Neurobit Optima+ 4 USB";
	protected override string DriverName => "NeurobitDrv64.dll";
	protected override string DriverPath => $@"..\..\..\Drivers\Neurobit Optima\{DriverName}";

	const string DefaultConfigName = "Default.cfg";

	public NeurobitOptima(string configFileName = DefaultConfigName)
	{
		IntPtr library = LoadLibrary(DriverPath);
		int channels;
		DevContextInfo devInfo = new DevContextInfo
		{
			model = DeviceName
		};
		int err = GetLastError();
		Console.WriteLine(err);
		if (library == 0x0)
		{
			MessageBox.Show($"Nie udało się wczytać biblioteki {DriverName}.", DeviceName);
		}

		//if ((DeviceContext = ReadCfgFile(configFileName)) < 0)
		{
			/* Cannot read last configuration.
				Open context for default device. */
			//Console.WriteLine($"Failed. Error Code : {GetLastError()});
			devInfo.deviceContext = NdOpenDevContext(DeviceName);
			if (devInfo.deviceContext < 0)
			{
				MessageBox.Show("Nie można otworzyć urządzenia", DeviceName, MessageBoxButton.OK,
					MessageBoxImage.Error);
				FreeLibrary(library);
			}
		}
		NDGETVAL getter;
		NDSETVAL setter;
		/* Get number of channels */
		if (NdGetParam(ParameterId("ND_PAR_CHAN_NUM"), 0, &getter) <= 0)
		{
			MessageBox.Show("Nie można pobrać liczby kanałów", DeviceName, MessageBoxButton.OK,
				MessageBoxImage.Error);
		}

		channels = getter.val.i;

		/* Example of automatic device configuration:
		Enable all versatile channels and set "EEG" profile for them. */
		setter.val.b = true;
		channels &= ~1; /* Even number of channels will be used in this example */
		List<string> channelsName = ["EEG"];
		for (short i = 0; i < channels; i++)
		{
			if (NdSetParam(ParameterId("ND_PAR_CH_EN"), i, &setter) < 0)
			{
				/* Device parameter can be set with NdSetParam... */
				MessageBox.Show("Nie można pobrać uruchomić kanału", DeviceName, MessageBoxButton.OK,
					MessageBoxImage.Error);
			}

			if (NdStr2Param(channelsName[i], ParameterId("ND_PAR_CH_PROF"), i) < 0)
			{
				/* ...or with NdStr2Param function */
				MessageBox.Show("Nie można ustawić profilu", DeviceName, MessageBoxButton.OK,
					MessageBoxImage.Error);
			}
		}
		/* Write data header for the device and prepare info for sample processing */
		AsciiWriteHeader(&devInfo);
		// Call into the native DLL, passing the managed callback
		int code = NdStartMeasurement(1, TStartMeasurement.MeasurementMode.Normal);
		if (code < 0)
			MessageBox.Show("Nie można połączyć z urządzeniem", DeviceName);
		if (code > 0)
			MessageBox.Show("Nie można rozpocząć pomiarów", DeviceName);
	}

	/* Write dump header. The function sets sample coefficients (int to real)
	and names for individual channels in the *dev structure for further use.
	Consecutive header columns are connected with consecutive signals.
	It returns number of channels for success, or zero for error. */
	static int AsciiWriteHeader(DevContextInfo* dev)
	{
		const int SIG_HEADER_LINES = 7;
		//const char*  const SigHeaderNames [SIG_HEADER_LINES] =  {
		string[] SigHeaderNames =
		{
			"Channel", "Min", "Max", "Unit", "SR", "Label", "Sensor"
		};
		//byte[] chan_en = new byte[MAX_SIGNALS];
		bool[] chan_en = new bool[MAX_SIGNALS];
		short i, l;
		short chans; /* Number of channels */
		short sig_num; /* Number of signals */
		NDGETVAL v;

		/* Set channel enable array and number of signals */
		if (NdGetParam(ParameterId("ND_PAR_CHAN_NUM"), 0, &v) == 0 || (v.type & ~ParameterId("ND_T_LIST")) != ParameterId("ND_T_INT"))
			return 0;
		chans = (short)v.val.i;
		for (i = sig_num = 0; i < chans; i++)
		{
			if (chans > 1)
			{
				if (NdGetParam(ParameterId("ND_PAR_CH_EN"), i, &v) == 0 || !((v.type & ~ParameterId("ND_T_LIST")) == ParameterId("ND_T_BOOL")))
					return 0;
				chan_en[i] = v.val.b;
			}
			else
				chan_en[i] = true;

			if (chan_en[i])
				sig_num++;
		}

		/* Print sample array header */
		for (l = 0; l < SIG_HEADER_LINES; l++)
		{
			Console.WriteLine(SigHeaderNames[l]);
			for (i = 0; i < chans; i++)
			{
				NDPARAM* p;

				if (!chan_en[i])
					continue;
				Console.WriteLine("\t");
				switch (l)
				{
					case 0:
						if (NdGetParam(ParameterId("ND_PAR_CH_NAME"), i, &v)==0 || !((v.type & ~ParameterId("ND_T_LIST")) == ParameterId("ND_T_TEXT")))
							return 0;
						Console.WriteLine(v.val.t);
						//Util.Memset(dev->names[i], 0, Marshal.SizeOf(dev->names[i]));
						dev->names = v.val.t;
						//strncpy(dev->names[i], v.val.t, Marshal.SizeOf(dev->names[i])-1);
						break;
					case 1:
						if (NdGetParam(ParameterId("ND_PAR_CH_RANGE_MIN"), i, &v)==0 || !((v.type & ~ParameterId("ND_T_LIST")) == ParameterId("ND_T_FLOAT")))
							return 0;
						Console.WriteLine(v.val.f);
						break;
					case 2:
						if (NdGetParam(ParameterId("ND_PAR_CH_RANGE_MAX"), i, &v)==0 || !((v.type & ~ParameterId("ND_T_LIST")) == ParameterId("ND_T_FLOAT")))
							return 0;
						Console.WriteLine(v.val.f);
						dev->coeff[i] = v.val.f / 0x80000000ul;
						break;
					case 3:
						p = NdParamInfo(ParameterId("ND_PAR_CH_RANGE_MAX"), i);
						Console.WriteLine(p->unit);
						break;
					case 4:
						if (NdGetParam(ParameterId("ND_PAR_CH_SR"), i, &v)==0 || !((v.type & ~ParameterId("ND_T_LIST")) == ParameterId("ND_T_FLOAT")))
							return 0;
						Console.WriteLine(v.val.f);
						break;
					case 5:
						if (NdGetParam(ParameterId("ND_PAR_CH_LABEL"), i, &v)==0 || !((v.type & ~ParameterId("ND_T_LIST")) == ParameterId("ND_T_TEXT")))
							return 0;
						Console.WriteLine(v.val.t);
						break;
					case 6:
						if (NdGetParam(ParameterId("ND_PAR_CH_TRANSDUCER"), i, &v)==0 || !((v.type & ~ParameterId("ND_T_LIST")) == ParameterId("ND_T_TEXT")))
							return 0;
						Console.WriteLine(v.val.t);
						break;
				}
			}

			Console.WriteLine("\n");
		}

		dev->dev_chans = chans;
		return chans;
	}

	private int ReadCfgFile(string fileName)
	{
		char[] buf = [];
		int r = 0;
		string cf = "";
		FileInfo fi = new FileInfo(fileName);
		if (!fi.Exists)
		{
			File.Create(fileName);
		}

		try
		{
			cf = File.ReadAllText(fileName);
		}
		catch
		{
			r = -1;
		}

		if (cf.Length != fi.Length)
			r = -2;
		//(short dc, void *buf, dword size)
		//cf = CreateFile(fname, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, 0, NULL);
		/*if (cf == INVALID_HANDLE_VALUE)
			return -1;
		if ((len=GetFileSize(cf, NULL)) == -1)
			r = -2;*/
		// else if (!(buf=malloc(len)))
		// 	r = -3;
		// else if (!ReadFile(cf, buf, len, &n, NULL) || len!=n)
		// 	r = -4;
		else if (NdSetDevConfig(-1, buf, cf.Length) < 0)
			r = -5;
		return r;
	}

	private static short ParameterId(string parameter)
	{
		ParameterIds a = (ParameterIds)Enum.Parse(typeof(ParameterIds), parameter);
		return (short)a;
	}
}