using SensorsInterface.Helpers;

namespace SensorsInterface.Devices.Simulator;

public class DeviceSimulator : Device
{
	public override string Name { get; set; } = "DeviceSimulator";
	public override string Code { get; set; } = "DeviceSimulator";
	public override List<string> SignalsAvailable { get; set; } = ["EKG", "BVP", "HRV", "GSR"];
	public override List<string> SignalsChosen { get; set; } = [];
	public override Dictionary<string, Dictionary<DateTime, double>> Signals { get; set; }
	protected override bool[] ChannelsEnable { get; set; } = [true, true, true, true];
	public override int ChannelsNumber { get; set; } = 4;
	public override List<double> Frequencies { get; set; } = [62.5, 125, 250, 500, 1000, 2000];
	public override List<string> ChannelFunctions { get; set; } = ["Voltage", "Conduction", "Temperature", "Resistance"];

	public override Dictionary<string, string> ChannelFunctionsPolish { get; set; } = new()
	{
		{"Voltage","Napięcie"},
		{"Conduction","Przewodność"},
		{"Temperature","Temperatura"},
		{"Resistance","Oporność"},
	};
	public override Dictionary<string, string> ChannelFunctionsUnits { get; set; } = new()
	{
		{"Voltage"," V"},
		{"Conduction"," S"},
		{"Temperature","\u00b0C"},
		{"Resistance","Ω"},
	};

	public override List<string> ChannelFunctionsChosen { get; set; } = [];
	protected override string DriverName { get; } = "";
	protected override string DriverPath { get; } = "";

	private SignalGenerator[] signalGenerator;
	private List<DateTime> lastTime = [];
	private List<TimeSpan> ticks = [];
	private Dictionary<string, int> signalFrequencies = new()
	{
		{"EKG",62},
		{"BVP",62},
		{"HRV",62},
		{"GSR",62},
	};
	private int gain = 40;
	private int offset = 50;

	public override Error.ErrorCode Initialize()
	{
		signalGenerator = new SignalGenerator[ChannelsNumber];
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

	public override Error.ErrorCode SetSignals(List<string> signals)
	{
		ticks.Clear();
		lastTime.Clear();
		DateTime now = DateTime.Now;
		for (int i = 0; i < signals.Count; i++)
		{
			signalGenerator[i] = new SignalGenerator(SignalType.Sine);
			if (i > 0) signalGenerator[i].Synchronize(signalGenerator[0]);
			ticks.Add(now.AddMicroseconds(1000000 / signalFrequencies[signals[i]]) - now);
			lastTime.Add(now);
		}
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
			if (now - lastTime[i] >= ticks[i])
			{
				if (!dateAdded)
				{
					message += $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()}@";
					dateAdded = true;
				}

				lastTime[i] = now;
				float value = gain * signalGenerator[i].GetValue() + offset;
				message += $"{SignalsChosen[i]}={value}#{i}";
			}
		}

		return message;
	}

	protected override string RetrieveFromNetwork()
	{
		throw new NotImplementedException();
	}

	protected override void SendByPipe(string message)
	{
		throw new NotImplementedException();
	}

	protected override void SendByNetwork(string message)
	{
		throw new NotImplementedException();
	}

	public override void ConvertValueToStandard()
	{
		//throw new NotImplementedException();
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