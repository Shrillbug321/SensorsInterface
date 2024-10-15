using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using static SensorsInterface.Native.NativeMethods;
using static SensorsInterface.Helpers.Error;

namespace SensorsInterface.Devices.NeurobitOptima;

public unsafe partial class NeurobitOptima : Device
{
	/*public override List<string> SignalsAvailable { get; set; } =
	[
		"EEG", "EKG", "EOG", "HRV", "SCP", "RESP", "nIR_HEG", "pIR_HEG", "BMP", "GSR", "RESP_BELT", "RESP_TEMP", "BVP",
		"EMG"
	];*/

	public override Dictionary<string, Signal> Signals { get; set; } = new()
	{
		["EEG"] = new Signal
		{
			Name = "EEG", Frequency = 62.5, Values = new Dictionary<DateTime, double>()
		},
		["EKG"] = new Signal
		{
			Name = "EKG", Frequency = 62.5, Values = new Dictionary<DateTime, double>()
		},
		["EOG"] = new Signal
		{
			Name = "EOG", Frequency = 62.5, Values = new Dictionary<DateTime, double>()
		},
		["HRV"] = new Signal
		{
			Name = "HRV", Frequency = 62.5, Values = new Dictionary<DateTime, double>()
		},
	};

	public override Dictionary<string, Signal> SignalsChosen { get; set; } = [];
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
	public override List<RangeState> RangeStates { get; set; }
	public override string Name { get; set; } = "Neurobit Optima+ 4 USB";
	public override string Code { get; set; } = "NeurobitOptima";
	protected override string DriverName => "NeurobitDrv64.dll";
	protected override string DriverPath => $@"..\..\..\Drivers\Neurobit Optima\{DriverName}";

	const string DefaultConfigName = "Default.cfg";

	private DevContextInfo devInfo;

	protected override List<bool> ChannelsEnable { get; set; } = [true, true, true, true];
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

	public NeurobitOptima()
	{
		SignalsChosen = new Dictionary<string, Signal>(Signals);
	}

	public override ErrorCode Initialize()
	{
		nint library = LoadLibrary(DriverPath);

		if (library == 0x0)
			return ErrorCode.LibraryNotLoaded;

		devInfo = new DevContextInfo
		{
			model = Name, coeff = new float[MAX_SIGNALS]
		};

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

		return GetChannelNumber();
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
		setter.val.b = true;
		return ErrorCode.Success;
	}

	public override ErrorCode SetSignals(List<Signal> signals)
	{
		for (short i = 0; i < signals.Count; i++)
		{
			if (NdSetParam(ParameterId("ND_PAR_CH_EN"), i, out setter) < 0)
				return ErrorCode.DeviceChannelNotRun;

			if (NdStr2Param(signals[i].Name, ParameterId("ND_PAR_CH_PROF"), i) < 0)
				return ErrorCode.DeviceProfileNotSet;
		}

		AsciiWriteHeader(ref devInfo);
		return ErrorCode.Success;
	}

	public override ErrorCode SetFrequency(string signal, double frequency)
	{
		throw new NotImplementedException();
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
		string message = "";
		DateTime now = DateTime.Now;
		List<string> signalHeaders = ["Channel", "Min", "Max", "Unit", "SR", "Label", "Sensor"];
		for (short i = 0; i < ChannelsNumber; i++)
		{
			if (!ChannelsEnable[i]) continue;

			string channelValue = "";
			foreach (string header in signalHeaders)
			{
				if (header != "Unit")
				{
					if (!IsValueProperlyGet(header, i))
					{
						if (ErrorCounter++ == 10)
							return $"Optima Error {header}";
					}
				}

				switch (header)
				{
					case "Channel":
						channelValue += getter.val.t + "Ch;";
						devInfo.names = getter.val.t;
						break;
					case "Min":
						channelValue += getter.val.f + "Min;";
						break;
					case "Max":
						channelValue += getter.val.f + "Max;";
						devInfo.coeff[i] = getter.val.f / 0x80000000ul;
						break;
					case "Unit":
						NDPARAM* p = NdParamInfo(ParameterId("ND_PAR_CH_RANGE_MAX"), i);
						channelValue += p->unit + "U;";
						break;
					case "SR":
						channelValue += getter.val.f + "SR;";
						break;
					case "Label":
						channelValue += getter.val.t + "L;";
						break;
					case "Sensor":
						channelValue += getter.val.t + "S;";
						break;
				}
			}

			float value = float.Parse(channelValue.Split("L;")[1][..^2]);
			message += $"{SignalsChosen.Values.First(s => s.Id == i).Name}={value}#{i}";
			SignalsChosen.Values.First(s => s.Id == i).Values.Add(now, value);
		}

		return message;
	}

	protected override string RetrieveFromNetwork()
	{
		//Część pobierająca dane wywołana z nadklasy
		string retrieved = base.RetrieveFromNetwork(false);
		if (retrieved == "")
		{
			ErrorCounter++;
			return "";
		}
		//Część konwertująca
		string message = "";
		char[] separator = { '=', '#' };
		string[] split = retrieved.Split('@');
		DateTimeOffset date = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(split[0])).DateTime.AddHours(2);

		string[] channels = split[1].Split(';', StringSplitOptions.RemoveEmptyEntries);
		for (int j = 0; j < channels.Length; j++)
		{
			if (!ChannelsEnable[j]) continue;
			string[] splitObservation = channels[j].Split(separator, StringSplitOptions.RemoveEmptyEntries);

			message += $"{date.ToString()}@{SignalsChosen.Values.First(s => s.Id == j).Name}={value}#{j}";
			SignalsChosen.Values.First(s => s.Id == j).Values.Add(date.DateTime, double.Parse(splitObservation[1]));
		}

		return message;
	}

	public override void Close()
	{
		NdStopMeasurement(devInfo.deviceContext);
		NdCloseDevContext(devInfo.deviceContext);
	}

	public override ErrorCode CheckDeviceState()
	{
		if (ErrorCounter >= 10)
		{
			Close();
			return ErrorCode.DeviceIsDisconnected;
		}

		return ErrorCode.Success;
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