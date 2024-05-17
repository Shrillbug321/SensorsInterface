using System.IO;
using System.Windows;
using static SensorsInterface.Native.NativeMethods;
using static SensorsInterface.Helpers.Error;

namespace SensorsInterface.Devices.NeurobitOptima;

public unsafe partial class NeurobitOptima : Device
{
	public override List<string> AvailableSignals { get; set; } = ["EKG", "RestTemp"];
	public override List<string> Signals { get; set; } = [];
	public override string DeviceName { get; set; } = "Neurobit Optima+ 4 USB";
	protected override string DriverName => "NeurobitDrv64.dll";
	protected override string DriverPath => $@"..\..\..\Drivers\Neurobit Optima\{DriverName}";

	const string DefaultConfigName = "Default.cfg";
	private DevContextInfo devInfo;
	private static bool[] channelsEnable = new bool[MAX_SIGNALS];
	private static short channelsNumber;
	private static NDGETVAL getter;
	private static NDSETVAL setter;
	private string value;

	private DevContextInfo* dev;

	private static Dictionary<string, (string, string, string)> gettersAssocations = new()
	{
		//AsciiWriteHeader
		{ "ChannelsNumber", ("ND_PAR_CHAN_NUM", "ND_T_LIST", "ND_T_INT") },
		{ "ChannelsEnable", ("ND_PAR_CH_EN", "ND_T_LIST", "ND_T_BOOL") },
		//Read
		{ "Channel", ("ND_PAR_CH_NAME", "ND_T_LIST", "ND_T_TEXT") },
		{ "Min", ("ND_PAR_CH_RANGE_MIN", "ND_T_LIST", "ND_T_FLOAT") },
		{ "Max", ("ND_PAR_CH_RANGE_MAX", "ND_T_LIST", "ND_T_FLOAT") },
		{ "SR", ("ND_PAR_CH_SR", "ND_T_LIST", "ND_T_FLOAT") },
		{ "Label", ("ND_PAR_CH_LABEL", "ND_T_LIST", "ND_T_TEXT") },
		{ "Sensor", ("ND_PAR_CH_TRANSDUCER", "ND_T_LIST", "ND_T_TEXT") },
	};

	public override ErrorCode Initialize()
	{
		/*State = DeviceState.Initialized;
		return ErrorCode.Success;*/
		nint library = LoadLibrary(DriverPath);
		devInfo = new DevContextInfo
		{
			model = DeviceName, coeff = new float[MAX_SIGNALS]
		};
		//dev = &devInfo;
		int err = GetLastError();
		Console.WriteLine(err);

		if (library == 0x0)
			return ErrorCode.LibraryNotLoaded;

		/*if ((DeviceContext = ReadCfgFile(DefaultConfigName)) < 0)
		{
			/*Cannot read last configuration.
				Open context for default device. #1#
			devInfo.deviceContext = NdOpenDevContext(DeviceName);
			if (devInfo.deviceContext < 0)
			{
				FreeLibrary(library);
				return ErrorCode.DeviceNotOpen;
			}
		}*/
		/* Get number of channels */
		if (NdGetParam(ParameterId("ND_PAR_CHAN_NUM"), 0, out getter) < 0)
			return ErrorCode.DeviceChannelsNotGet;

		int channels = getter.val.i;

#if DEBUG
		channels = AvailableSignals.Count;
#endif

		/* Example of automatic device configuration:
		Enable all versatile channels and set "EEG" profile for them. */
		setter.val.b = true;
		//channels &= ~1; /* Even number of channels will be used in this example #1#

		for (short i = 0; i < channels; i++)
		{
			if (NdSetParam(ParameterId("ND_PAR_CH_EN"), i, out setter) < 0)
				return ErrorCode.DeviceChannelNotRun;

			if (NdStr2Param(AvailableSignals[i], ParameterId("ND_PAR_CH_PROF"), i) < 0)
				return ErrorCode.DeviceProfileNotSet;
		}

		/* Write data header for the device and prepare info for sample processing */
		AsciiWriteHeader(ref devInfo);
		//return ErrorCode.Success;
		///*
		// Call into the native DLL, passing the managed callback
		int code = NdStartMeasurement(1, TStartMeasurement.MeasurementMode.Normal);

		return ErrorCode.Success;

		/*return code switch
		{
			< 0 => ErrorCode.DeviceNotConnected,
			> 0 => ErrorCode.DeviceMeasurementCannotStart,
			0 => ErrorCode.Success
		};*/
		//*/
	}

	/*public ErrorCode SetSignals(List<string> signals)
	{
		
	}*/
	
	/* Write dump header. The function sets sample coefficients (int to real)
	and names for individual channels in the *dev structure for further use.
	Consecutive header columns are connected with consecutive signals.
	It returns number of channels for success, or zero for error. */
	private int AsciiWriteHeader(ref DevContextInfo dev)
	{
		/* Set channel enable array and number of signals */
		if (!IsValueProperlyGet("ChannelsNumber", 0))
			return 0;
		channelsNumber = (short)getter.val.i;
#if DEBUG
		channelsNumber = (short)AvailableSignals.Count;
#endif
		for (short i = 0; i < channelsNumber; i++)
		{
			if (channelsNumber > 1)
			{
				if (!IsValueProperlyGet("ChannelsEnable", i))
					return 0;
				channelsEnable[i] = getter.val.b;
			}
			else
				channelsEnable[i] = true;
		}

		dev.dev_chans = channelsNumber;
		return channelsNumber;
	}

	public override string Read()
	{
		bool foundError = false;
		value = "";
		//const char*  const SigHeaderNames [SIG_HEADER_LINES] =  {
		List<string> signalHeaders = ["Channel", "Min", "Max", "Unit", "SR", "Label", "Sensor"];

		/* Print sample array header */
		foreach (string header in signalHeaders)
		{
			Console.WriteLine(header);
			for (short i = 0; i < channelsNumber; i++)
			{
				if (!channelsEnable[i])
					continue;

				if (header != "Unit")
				{
					if (!IsValueProperlyGet(header, i))
					{
						foundError = true;
						continue;
					}
				}

				switch (header)
				{
					case "Channel":
						value += getter.val.t + ";";
						dev->names = getter.val.t;
						break;
					case "Min":
						value += getter.val.f + ";";
						break;
					case "Max":
						value += getter.val.f + ";";
						dev->coeff[i] = getter.val.f / 0x80000000ul;
						break;
					case "Unit":
						NDPARAM* p = NdParamInfo(ParameterId("ND_PAR_CH_RANGE_MAX"), i);
						value += p->unit + ";";
						break;
					case "SR":
						value += getter.val.f + ";";
						break;
					case "Label":
						value += getter.val.t + ";";
						break;
					case "Sensor":
						value += getter.val.t + ";";
						break;
				}

				if (foundError)
				{
					ShowMessageBox(ErrorCode.DeviceMeasurementReadError);
					return $"Optima Error {header}";
				}
			}
		}

		Console.WriteLine(value + "\n");
		return value;
	}

	public override string StandardizeValue()
	{
		StandardizedValue = $"{DateTime.Now}/" +
		                    $"81|1";
		//data
		//wartość|typ
		//
		//;
		/*StandardizedValue = $"{DateTime.Now}\n";
		string[] channels = value.Split('\n');
		StandardizedValue += "[";
		int i = 0;
		foreach (string channel in channels)
		{
			string[] split = channel.Split(';');
			//zależy od read()
			//Format- wartość;typ (czyli numer kanału),
			StandardizedValue += $"{split[1]}|{i++}\n";
		}*/

		return StandardizedValue += ";";
	}

	public override void Close()
	{
		NdStopMeasurement(devInfo.deviceContext);
		NdCloseDevContext(devInfo.deviceContext);
	}

	private static bool IsValueProperlyGet(string value, int channelNumber)
	{
		var tuple = gettersAssocations[value];

#if DEBUG
		return NdGetParam(ParameterId(tuple.Item1), channelNumber, out getter) != 0 ||
		       (getter.type & ~TypeId(tuple.Item2)) != TypeId(tuple.Item3);
#endif

		return NdGetParam(ParameterId(tuple.Item1), channelNumber, out getter) == 0 ||
		       (getter.type & ~TypeId(tuple.Item2)) == TypeId(tuple.Item3);

		//Ten warunek sprawdzał czy jest niepoprawne
		/*return NdGetParam(ParameterId(tuple.Item1), channelNumber, &getter) == 0 ||
		       !((getter.type & ~ParameterId(tuple.Item2)) == ParameterId(tuple.Item3));*/
	}

	private int ReadCfgFile(string fileName)
	{
		char[] buf = [];
		int r = 0;
		string cf = "";
		FileInfo fi = new FileInfo(fileName);
		if (!fi.Exists)
			File.Create(fileName);

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

	private static short TypeId(string type)
	{
		TypeIds a = (TypeIds)Enum.Parse(typeof(TypeIds), type);
		return (short)a;
	}
}