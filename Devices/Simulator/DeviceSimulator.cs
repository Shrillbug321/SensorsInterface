using System.Net;
using System.Text;
using SensorsInterface.Helpers;

namespace SensorsInterface.Devices.Simulator;

public class DeviceSimulator : Device
{
	public override string Name { get; set; } = "DeviceSimulator";
	public override string Code { get; set; } = "DeviceSimulator";
	public override Dictionary<string, Signal> Signals { get; set; } = new()
	{
		["BPM"] = new Signal
		{
			Name = "BPM", Frequency = 62.5, Values = new Dictionary<DateTime, double>()
		},
		["GSR"] = new Signal
		{
			Name = "GSR", Frequency = 62.5, Values = new Dictionary<DateTime, double>()
		},
		["BVP"] = new Signal
		{
			Name = "BVP", Frequency = 62.5, Values = new Dictionary<DateTime, double>()
		},
		["EMG"] = new Signal
		{
			Name = "EMG", Frequency = 62.5, Values = new Dictionary<DateTime, double>()
		}
	};

	public override Dictionary<string, Signal> SignalsChosen { get; set; } = [];

	protected override List<bool> ChannelsEnable { get; set; } = [true, true, true, true];
	public override int ChannelsNumber { get; set; } = 4;
	public override List<double> Frequencies { get; set; } = [62.5, 125, 250, 500, 1000, 2000];

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

	public override List<string> ChannelFunctionsChosen { get; set; } = ["Voltage", "Voltage", "Voltage", "Voltage"];
	public override List<RangeState> RangeStates { get; set; }
	protected override string DriverName { get; } = "";
	protected override string DriverPath { get; } = "";

	private SignalGenerator[] signalGenerator;
	private List<DateTime> lastTime = [..new DateTime[4]];
	private List<TimeSpan> ticks = [];

	private int gain = 40;
	private int offset = 50;

	public DeviceSimulator()
	{
		SignalsChosen = new Dictionary<string, Signal>(Signals);
        SignalsChosen.Remove("RESP_TEMP");
        SignalsChosen.Remove("RESP_BELT");
        SignalsChosen["BPM"].Id = 0;
        SignalsChosen["GSR"].Id = 1;
        SignalsChosen["BVP"].Id = 2;
        SignalsChosen["EMG"].Id = 3;
	}
	
	public override Error.ErrorCode Initialize()
	{
		signalGenerator = new SignalGenerator[ChannelsNumber];
		RangeStates = [..new RangeState[4]];
		DateTime now = DateTime.Now;
		for (int i = 0; i < SignalsChosen.Count; i++)
		{
			signalGenerator[i] = new SignalGenerator(SignalType.Linear)
			{
				Frequency = float.Parse(SignalsChosen.FindValueByIndex(i).Frequency.ToString())
			};
			ticks.Add(now.AddMicroseconds(1000000 / SignalsChosen.FindValueByIndex(i).Frequency) - now);
			lastTime.Add(now);
		}

		return Error.ErrorCode.Success;
	}

	public override Error.ErrorCode SetSignal(string signals)
	{
		throw new NotImplementedException();
	}

	public override Error.ErrorCode SetUnit(string unit)
	{
		throw new NotImplementedException();
	}

	public override Error.ErrorCode SetChannelFunction(string channelFunction)
	{
		throw new NotImplementedException();
	}

	public override Error.ErrorCode SetSignals(List<Signal> signals)
	{
		ticks.Clear();
		lastTime.Clear();
		DateTime now = DateTime.Now;
		for (int i = 0; i < signals.Count; i++)
		{
			signalGenerator[i] = new SignalGenerator(SignalType.Sine);
			if (i > 0) signalGenerator[i].Synchronize(signalGenerator[0]);
			ticks.Add(now.AddMicroseconds(1000000 / signals[i].Frequency) - now);
			lastTime.Add(now);
		}

		return Error.ErrorCode.Success;
	}

	public override Error.ErrorCode SetFrequency(string signal, double frequency)
	{
		DateTime now = DateTime.Now;
		int i = SignalsChosen.FindIndexByKey(signal);
		SignalsChosen[signal].Frequency = frequency;
		signalGenerator[i] = new SignalGenerator(SignalType.Sine)
		{
			Frequency = float.Parse(SignalsChosen.FindValueByIndex(i).Frequency.ToString())
		};
		ticks[i] = now.AddMicroseconds(1000000 / frequency) - now;
		return Error.ErrorCode.Success;
	}

	public override Error.ErrorCode StartMeasurement()
	{
		return Error.ErrorCode.Success;
	}

	protected override string RetrieveFromDriver()
	{
		DateTime now = DateTime.Now;
		bool dateAdded = false;
		string message = "";
		for (int i = 0; i < SignalsChosen.Count; i++)
		{
			if (!ChannelsEnable[i]) continue;
			if (now - lastTime[i] >= ticks[i])
			{
				if (!dateAdded)
				{
					message += $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()}@";
					dateAdded = true;
				}

				lastTime[i] = now;
				float value = gain * signalGenerator[i].GetValue() + offset;
				message += $"{SignalsChosen.Values.First(s=>s.Id == i).Name}={value}#{i}";
				SignalsChosen.Values.First(s=>s.Id == i).Values.Add(now, value);
			}
		}

		return message;
	}

	protected override string RetrieveFromNetwork()
	{
		//Część pobierająca dane wywołana z nadklasy
		string retrieved = base.RetrieveFromNetwork(false);
		//Część konwertująca
		string[] split = retrieved.Split('@');
		DateTimeOffset date = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(split[0]));
		string[] channels = split[1].Split(';')[..^1];
		DateTime date2 = date.DateTime.AddHours(2);
		Console.WriteLine(date2);
		for (int j = 0; j < channels.Length; j++)
		{
			string[] s = channels[j].Split('=');
			if (!ChannelsEnable[j]) continue;
			s[1] = s[1].Split('#')[0];
			if (!SignalsChosen.ContainsKey(s[0])) continue;
			SignalsChosen[s[0]].Values.Add(date2, double.Parse(s[1]));
		}
		
		return retrieved;
	}

	public override void Close()
	{
		//Simulator don't need closing
	}

	private int disconnect;
	public override Error.ErrorCode CheckDeviceState()
	{
		if (State == DeviceState.Initialized) ErrorCounter++;
		//return disconnect++ >= 20 ? Error.ErrorCode.DeviceIsDisconnected : Error.ErrorCode.Success;
		return Error.ErrorCode.Success;
		//return Error.ErrorCode.DeviceIsDisconnected;
	}
}