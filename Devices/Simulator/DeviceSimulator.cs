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
		["EKG"] = new Signal
		{
			Name = "EKG", Frequency = 62.5, Values = new Dictionary<DateTime, double>()
		},
		["BVP"] = new Signal
		{
			Name = "BVP", Frequency = 62.5, Values = new Dictionary<DateTime, double>()
		},
		["HRV"] = new Signal
		{
			Name = "HRV", Frequency = 62.5, Values = new Dictionary<DateTime, double>()
		},
		["GSR"] = new Signal
		{
			Name = "GSR", Frequency = 62.5, Values = new Dictionary<DateTime, double>()
		},
	};

	public override Dictionary<string, Signal> SignalsChosen { get; set; }
	public override Dictionary<string, Signal> SignalsAvailable { get; set; } = [];

	protected override bool[] ChannelsEnable { get; set; } = [true, true, true, true];
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

	public override Error.ErrorCode Initialize()
	{
		SignalsChosen = new Dictionary<string, Signal>(Signals);
		SignalsChosen["EKG"].Id = 0;
		SignalsChosen["BVP"].Id = 1;
		SignalsChosen["HRV"].Id = 2;
		SignalsChosen["GSR"].Id = 3;
		signalGenerator = new SignalGenerator[ChannelsNumber];
		//ChannelFunctionsChosen = [..new string[4]];
		RangeStates = [..new RangeState[4]];
		DateTime now = DateTime.Now;
		for (int i = 0; i < SignalsChosen.Count; i++)
		{
			signalGenerator[i] = new SignalGenerator(SignalType.Sine);
			if (i > 0) signalGenerator[i].Synchronize(signalGenerator[0]);
			ticks.Add(now.AddMicroseconds(1000000 / SignalsChosen.FindByIndex(i).Frequency) - now);
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
		//SignalsChosen.Find()
		int i = SignalsChosen[signal].Id;
		SignalsChosen[signal].Frequency = frequency;
		signalGenerator[i] = new SignalGenerator(SignalType.Sine);
		if (i > 0) signalGenerator[i].Synchronize(signalGenerator[0]);
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
		IPEndPoint endPoint = IpEndPoints[retrievePort];
		//IpEndPoints[retrievePort] = endPoint;
		string retrieved = Encoding.ASCII.GetString(sockets[retrievePort].Receive(ref endPoint));
		
		string[] split = retrieved.Split('@');
		DateTimeOffset date = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(split[0]));
		string[] channels = split[1].Split(';')[..^1];
		DateTime date2 = date.DateTime.AddHours(2);
		Console.WriteLine(date2);
		for (int j = 0; j < channels.Length; j++)
		{
			string[] s = channels[j].Split('=');
			if (!ChannelsEnable[j]) continue; // || !SignalsChosen.Contains(s[0])) continue;
			s[1] = s[1].Split('#')[0];
			if (!SignalsChosen.ContainsKey(s[0])) continue;
			SignalsChosen[s[0]].Values.Add(date.DateTime, double.Parse(s[1]));
		}
		
		return retrieved;
	}

	protected override void SendByPipe(string message)
	{
		pipeWriter.WriteLine(message);
	}

	protected override void SendByNetwork(string message)
	{
		byte[] sendBytes = Encoding.ASCII.GetBytes(message);
		sockets[sendPort].Connect("localhost", sendPort);
		sockets[sendPort].Send(sendBytes, sendBytes.Length);
	}

	public override void ConvertValueToStandard()
	{
		StandardizedValue = "[";
		for (int i = 0; i < SignalsChosen.Count; i++)
		{
			Signal signal = Signals.FindByIndex(i);
			if (signal.Values.Count == 0) continue;
			KeyValuePair<DateTime, double> pair = signal.Values.Last();
			StandardizedValue += FHIR.FHIR.CreateObservation(signal.Name, pair.Value, pair.Key,
				ChannelFunctionsUnits[ChannelFunctionsChosen[i]], RangeStates[i].ToString()) + ",\n";
		}

		StandardizedValue += "]@#";
		//FHIR.FHIR.GetObservationsFromText(StandardizedValue);
	}

	public override string ConvertValueToStandardString()
	{
		throw new NotImplementedException();
	}

	public override void Close()
	{
		throw new NotImplementedException();
	}
}