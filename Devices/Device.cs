using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Media;
using SensorsInterface.Helpers;
using static SensorsInterface.Helpers.Error;
using static SensorsInterface.Global;

namespace SensorsInterface.Devices;

public abstract class Device
{
	public abstract string Name { get; set; }
	public abstract string Code { get; set; }
	public abstract Dictionary<string, Signal> Signals { get; set; }
	public abstract Dictionary<string, Signal> SignalsChosen { get; set; }
	public string StandardizedValue { get; protected set; } = "";
	protected abstract List<bool> ChannelsEnable{ get; set; }
	public abstract int ChannelsNumber { get; set; }
	public abstract List<double> Frequencies { get; set; }
	public abstract List<string> ChannelFunctions { get; set; }
	public abstract Dictionary<string,string> ChannelFunctionsPolish { get; set; }
	public abstract Dictionary<string,string> ChannelFunctionsUnits { get; set; }
	public abstract List<string> ChannelFunctionsChosen { get; set; }
	public abstract List<RangeState> RangeStates { get; set; }
	public enum DeviceState
	{
		None,
		Loaded,
		Initialized,
		Working,
		Stopped,
		Error
	}

	public enum RangeState
	{
		Low,Normal,High
	}
	
	private DeviceState state = DeviceState.None;

	public DeviceState State
	{
		get => state;
		set
		{
			state = value;
			StateColor = window.StateColors[value];
		}
	}

	public SolidColorBrush StateColor = new(Color.FromRgb(106, 128, 0));

	public RetrieveDataMode RetrieveData = RetrieveDataMode.None;
	public SendDataMode SendData = SendDataMode.None;

	public enum RetrieveDataMode
	{
		Driver,
		Network,
		None
	}

	public enum SendDataMode
	{
		Pipe,
		Network,
		None
	}

	protected Dictionary<int, UdpClient> sockets = new();
	protected Dictionary<int, IPEndPoint> IpEndPoints = new();
	public int retrievePort = 8053;
	public int sendPort = 8054;
	private NamedPipeServerStream? pipe;
	protected StreamWriter pipeWriter;
	protected abstract string DriverName { get; }
	protected abstract string DriverPath { get; }

	protected int DeviceContext { get; set; }
	public int ErrorCounter { get; set; }
	public abstract ErrorCode Initialize();
	public abstract ErrorCode SetSignal(string signals);
	public abstract ErrorCode SetUnit(string unit);
	public abstract ErrorCode SetChannelFunction(string channelFunction);
	public abstract ErrorCode SetSignals(List<Signal> signals);
	public abstract ErrorCode SetFrequency(string signal, double frequency);
	public abstract ErrorCode StartMeasurement();
	public abstract ErrorCode CheckDeviceState();

	public string Retrieve()
	{
		return RetrieveData switch
		{
			RetrieveDataMode.Driver => RetrieveFromDriver(),
			RetrieveDataMode.Network => RetrieveFromNetwork(),
			_ => $"Nie wybrano trybu czytania dla {Name}"
		};
	}

	public void SetChannelState(int channelNumber, bool newState)
	{
		ChannelsEnable[channelNumber] = newState;
	}
	protected abstract string RetrieveFromDriver();

	protected virtual string RetrieveFromNetwork()
	{
		IPEndPoint endPoint = IpEndPoints[retrievePort];
		return Encoding.ASCII.GetString(sockets[retrievePort].Receive(ref endPoint));
	}
	
	protected virtual string RetrieveFromNetwork(bool convert)
	{
		IPEndPoint endPoint = IpEndPoints[retrievePort];
		string retrieved = Encoding.ASCII.GetString(sockets[retrievePort].Receive(ref endPoint));
		
		if (convert)
			DefaultConverter(retrieved);
		
		return retrieved;
	}

	protected void DefaultConverter(string retrieved)
	{
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
			SignalsChosen[s[0]].Values.Add(date.DateTime, double.Parse(s[1]));
		}
	}
	
	public void Send(string message)
	{
		switch (SendData)
		{
			case SendDataMode.Pipe:
				SendByPipe(message);
				break;
			case SendDataMode.Network:
				SendByNetwork(message);
				break;
			case SendDataMode.None:
			default:
				break;
		}
	}

	protected virtual void SendByPipe(string message)
	{
		pipeWriter.WriteLine(message);
	}

	protected virtual void SendByNetwork(string message)
	{
		byte[] sendBytes = Encoding.ASCII.GetBytes(message);
		sockets[sendPort].Connect("localhost", sendPort);
		sockets[sendPort].Send(sendBytes, sendBytes.Length);
	}
	
	public abstract void Close();
	
	public void ConvertValueToStandard()
	{
		StandardizedValue = "[";
		for (int i = 0; i < SignalsChosen.Count; i++)
		{
			Signal signal = Signals.FindValueByIndex(i);
			if (signal.Values.Count == 0) continue;
			KeyValuePair<DateTime, double> pair = signal.Values.Last();
			StandardizedValue += FHIR.FHIR.CreateJsonObservation(signal.Name, pair.Value, pair.Key,
				ChannelFunctionsUnits[ChannelFunctionsChosen[i]], RangeStates[i].ToString()) + ",\n";
		}

		StandardizedValue += "]@#";
	}
	public ErrorCode CreateSocket(string mode)
	{
		int port = mode == "Retrieve" ? retrievePort : sendPort;
		return CreateSocket(port);
	}

	private ErrorCode CreateSocket(int port)
	{
		if (sockets.ContainsKey(port))
			return ErrorCode.Success;
		
		UdpClient socket = new UdpClient();
		IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
		sockets.Add(port, socket);
		IpEndPoints.Add(port, endPoint);
		try
		{
			socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			socket.Client.Bind(endPoint);
		}
		catch (Exception e)
		{
			return ErrorCode.SocketNotConnected;
		}

		return ErrorCode.Success;
	}
	public async Task<ErrorCode> CreatePipe(string name="VRTPipe")
	{
		if (pipe == null) 
		{
			pipe = new NamedPipeServerStream(name);
			pipeWriter = new StreamWriter(pipe);
		} 
		
		if (pipe.IsConnected)
			return ErrorCode.Success;
		
		return await Task.Run(() =>
		{
			pipe.WaitForConnectionEx();
			return pipe.IsConnected ? ErrorCode.Success : ErrorCode.PipeNotConnected;
		});
	}

	public string AddSignalChosen(string signal, int index, int indexOfChangedChannel)
	{
		if (SignalsChosen.ContainsKey(signal))
		{
			ShowMessageBox(ErrorCode.SignalIsChosen, signal);
			return SignalsChosen.FindKeyByIndex(indexOfChangedChannel);
		}
		SignalsChosen.Remove(SignalsChosen.FindKeyByIndex(indexOfChangedChannel));
		SignalsChosen.Add(signal, Signals[signal]);
		SignalsChosen[signal].Id = index;
		if (RetrieveData == RetrieveDataMode.Driver)
			SetSignals(SignalsChosen.Values.ToList());
		return "";
	}
}