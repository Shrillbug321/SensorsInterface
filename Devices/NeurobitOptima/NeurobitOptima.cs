using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using static SensorsInterface.Native.NativeMethods;
using static SensorsInterface.Helpers.Error;

namespace SensorsInterface.Devices.NeurobitOptima;

public unsafe partial class NeurobitOptima : Device
{
	public override List<string> SignalsAvailable { get; set; } =
	[
		"EEG", "EKG", "HRV", "SCP", "BMP", "GSR", "RESP_TEMP", "BVP", "EMG"
	];

	/*public override List<string> SignalsAvailable { get; set; } =
	[
		"EEG", "EKG", "EOG", "HRV", "SCP", "RESP", "nIR_HEG", "pIR_HEG", "BMP", "GSR", "RESP_BELT", "RESP_TEMP", "BVP",
		"EMG"
	];*/

	public override List<string> SignalsChosen { get; set; } = [];
	public override Dictionary<string, Dictionary<DateTime, double>> Signals { get; set; }
	public override List<double> Frequencies { get; set; } = [1, 2, 5, 10, 20, 50, 100, 200, 500];

	public override List<string> ChannelFunctions { get; set; } =
		["Voltage", "Conduction", "Temperature", "Resistance"];

	public override Dictionary<string, string> ChannelFunctionsPolish { get; set; } = new()
	{
		{ "Voltage", "Napięcie" },
		{ "Conduction", "Przewodność" },
		{ "Temperature", "Temperatura" },
		{ "Resistance", "Oporność" },
	};

	public override Dictionary<string, string> ChannelFunctionsUnits { get; set; } = new()
	{
		{ "Voltage", " V" },
		{ "Conduction", " S" },
		{ "Temperature", "\u00b0C" },
		{ "Resistance", "Ω" },
	};

	public override List<string> ChannelFunctionsChosen { get; set; } = [];
	public override string Name { get; set; } = "Neurobit Optima+ 4 USB";
	public override string Code { get; set; } = "NeurobitOptima";
	protected override string DriverName => "NeurobitDrv64.dll";
	protected override string DriverPath => $@"..\..\..\Drivers\Neurobit Optima\{DriverName}";

	const string DefaultConfigName = "Default.cfg";

	private DevContextInfo devInfo;

	//private static bool[] ChannelsEnable = new bool[MAX_SIGNALS];
	protected override bool[] ChannelsEnable { get; set; } = [true, true, true, true];

	public override int ChannelsNumber { get; set; } = 4;

	//Fields only for Optima
	private static NDGETVAL getter;
	private static NDSETVAL setter;
	private string value;

	private DevContextInfo* dev;
	private UdpClient udpClient;
	private IPEndPoint remoteIpEndPoint;

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
		//InitSocket();
		/*State = DeviceState.Initialized;
		return ErrorCode.Success;*/
		nint library = LoadLibrary(DriverPath);
		devInfo = new DevContextInfo
		{
			model = Name, coeff = new float[MAX_SIGNALS]
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
			devInfo.deviceContext = NdOpenDevContext(Name);
			if (devInfo.deviceContext < 0)
			{
				FreeLibrary(library);
				return ErrorCode.DeviceNotOpen;
			}
		}*/
		ErrorCode code = GetChannelNumber();

		if (code != ErrorCode.Success) return code;

		return ErrorCode.Success;
	}

	public override ErrorCode SetSignal(string signals)
	{
		throw new NotImplementedException();
	}

	public override ErrorCode SetUnit(string unit)
	{
		throw new NotImplementedException();
	}

	public override ErrorCode SetChannelFunction(string channelFunction)
	{
		throw new NotImplementedException();
	}

	public ErrorCode GetChannelNumber()
	{
		/* Get number of channels */
		if (NdGetParam(ParameterId("ND_PAR_CHAN_NUM"), 0, out getter) < 0)
			return ErrorCode.DeviceChannelsNotGet;

		ChannelsNumber = getter.val.i;

#if DEBUG
		ChannelsNumber = 4;
#endif

		/* Example of automatic device configuration:
		Enable all versatile channels and set "EEG" profile for them. */
		setter.val.b = true;

		return ErrorCode.Success;
	}

	public override ErrorCode SetSignals(List<string> signals)
	{
		for (short i = 0; i < signals.Count; i++)
		{
			if (NdSetParam(ParameterId("ND_PAR_CH_EN"), i, out setter) < 0)
				return ErrorCode.DeviceChannelNotRun;

			if (NdStr2Param(signals[i], ParameterId("ND_PAR_CH_PROF"), i) < 0)
				return ErrorCode.DeviceProfileNotSet;
		}

		/* Write data header for the device and prepare info for sample processing */
		AsciiWriteHeader(ref devInfo);
		return ErrorCode.Success;
	}

	public override ErrorCode StartMeasurement()
	{
		int code = NdStartMeasurement(1, TStartMeasurement.MeasurementMode.Normal);

		return code switch
		{
			< 0 => ErrorCode.DeviceNotConnected,
			> 0 => ErrorCode.DeviceMeasurementCannotStart,
			0 => ErrorCode.Success
		};
	}

	/*private void InitSocket()
	{
		udpClient = new();
		remoteIpEndPoint = new IPEndPoint(IPAddress.Any, 8054);
		try
		{
			udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			udpClient.Client.Bind(remoteIpEndPoint);
		}
		catch (Exception e)
		{
			Console.WriteLine(e.ToString());
		}
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
		ChannelsNumber = (short)getter.val.i;
#if DEBUG
		ChannelsNumber = 4;
#endif
		for (short i = 0; i < ChannelsNumber; i++)
		{
			if (ChannelsNumber > 1)
			{
				if (!IsValueProperlyGet("ChannelsEnable", i))
					return 0;
				//ChannelsEnable[i] = getter.val.b;
			}
			else
				ChannelsEnable[i] = true;
		}

		dev.dev_chans = ChannelsNumber;
		return ChannelsNumber;
	}

	protected override string RetrieveFromDriver()
	{
		bool foundError = false;
		value = "";
		List<string> signalHeaders = ["Channel", "Min", "Max", "Unit", "SR", "Label", "Sensor"];
		for (short i = 0; i < ChannelsNumber; i++)
		{
			Console.WriteLine($"Channel {i}");
			foreach (string header in signalHeaders)
			{
				if (!ChannelsEnable[i])
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
						value += getter.val.t + "Ch;";
						devInfo.names = getter.val.t;
						break;
					case "Min":
						value += getter.val.f + "Min;";
						break;
					case "Max":
						value += getter.val.f + "Max;";
						devInfo.coeff[i] = getter.val.f / 0x80000000ul;
						break;
					case "Unit":
						/*NDPARAM* p = NdParamInfo(ParameterId("ND_PAR_CH_RANGE_MAX"), i);
						value += p->unit + ";";*/
						value += "U;";
						break;
					case "SR":
						value += getter.val.f + "SR;";
						break;
					case "Label":
						value += getter.val.t + "L;";
						break;
					case "Sensor":
						value += getter.val.t + "S;";
						break;
				}

				if (foundError)
				{
					ShowMessageBox(ErrorCode.DeviceMeasurementReadError);
					return $"Optima Error {header}";
				}
			}

			value += "\r\n";
		Console.WriteLine(value);
		}

		return value;
	}

	private bool endPointCreated = false;

	protected override string RetrieveFromNetwork()
	{
		//if (!endPointCreated)
		IPEndPoint endPoint = IpEndPoints[retrievePort];
		//byte[] bytes = ;
		//if ()
		IpEndPoints[retrievePort] = endPoint;
		value = Encoding.ASCII.GetString(sockets[retrievePort].Receive(ref endPoint));
		return value;
	}

	protected override void SendByPipe(string message)
	{
		pipeWriter.WriteLine(message);
	}

	protected override void SendByNetwork(string message)
	{
		byte[] sendBytes = Encoding.ASCII.GetBytes(message);
		sockets[sendPort].Send(sendBytes, sendBytes.Length);
	}

	public override void ConvertValueToStandard()
	{
		/*string[] split = value.Split('@');
		DateTimeOffset date = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(split[0]));
		string[] channels = split[1].Split(';');
		DateTime date2 = date.DateTime.AddHours(2);
		Console.WriteLine(date2);
		for (int j = 0; j < channels.Length; j++)
		{
			string[] s = channels[j].Split('=');
			if (!ChannelsEnable[j]) continue; // || !SignalsChosen.Contains(s[0])) continue;
			s[1] = s[1].Split('#')[0];
			Signals[s[0]] = new Dictionary<DateTime, double>
			{
				{ date2, double.Parse(s[1]) }
			};
			Console.WriteLine($"{s[0]}={s[1]}");
		}*/
	}

	public override string ConvertValueToStandardString()
	{
		/*StandardizedValue = $"{DateTime.Now}/" +
		                    $"81|1";*/
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
		/*string[] split = value.Split('@');
		DateTimeOffset date = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(split[0]));
		string[] channels = split[1].Split(';');
		for (int j = 0; j < channels.Length; j++)
		{
			if (!ChannelsEnable[j]) continue;
			string[] s = channels[j].Split('=');
			s[1] = s[1].Split('#')[0];
			Signals[s[0]] = new Dictionary<DateTime, double>
			{
				{ date.DateTime, double.Parse(s[1]) }
			};
		}*/

		return Signals.ToString();
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