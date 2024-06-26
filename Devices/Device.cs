using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Windows.Media;
using SensorsInterface.Helpers;
using static SensorsInterface.Helpers.Error;
using static SensorsInterface.Global;

namespace SensorsInterface.Devices;

public abstract class Device
{
	public abstract string Name { get; set; }
	public abstract string Code { get; set; }
	public abstract List<string> SignalsAvailable { get; set; }
	public abstract List<string> SignalsChosen { get; set; }
	public abstract Dictionary<string, Dictionary<DateTime, double>> Signals { get; set; }
	//public abstract Dictionary<string, double> SignalsFrequencies { get; set; }
	public Dictionary<string, double> SignalsValues { get; } = [];
	public string StandardizedValue { get; protected set; } = "";
	protected abstract bool[] ChannelsEnable{ get; set; }
	public abstract int ChannelsNumber { get; set; }
	public abstract List<double> Frequencies { get; set; }
	public abstract List<string> ChannelFunctions { get; set; }
	public abstract Dictionary<string,string> ChannelFunctionsPolish { get; set; }
	public abstract Dictionary<string,string> ChannelFunctionsUnits { get; set; }
	public abstract List<string> ChannelFunctionsChosen { get; set; }
	public enum DeviceState
	{
		None,
		Loaded,
		Initialized,
		Working,
		Stopped,
		Error
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

	public SolidColorBrush StateColor = new(Color.FromRgb(0, 128, 0));

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
	public Dictionary<int, UdpClient> sockets = new();
	public Dictionary<int, IPEndPoint> IpEndPoints = new();
	public int retrievePort = 8053;
	public int sendPort = 8054;
	private NamedPipeServerStream? pipe;
	protected StreamWriter pipeWriter;
	protected abstract string DriverName { get; }
	protected abstract string DriverPath { get; }

	protected int DeviceContext { get; set; }

	protected Device()
	{
		foreach (string signalName in SignalsAvailable)
		{
			SignalsValues.Add(signalName, 0.0);
		}

		Signals = new Dictionary<string, Dictionary<DateTime, double>>();
	}

	public abstract ErrorCode Initialize();
	public abstract ErrorCode SetSignal(string signals);
	public abstract ErrorCode SetUnit(string unit);
	public abstract ErrorCode SetChannelFunction(string channelFunction);
	public abstract ErrorCode SetSignals(List<string> signals);
	public abstract ErrorCode StartMeasurement();

	public virtual string Retrieve()
	{
		return RetrieveData switch
		{
			RetrieveDataMode.Driver => RetrieveFromDriver(),
			RetrieveDataMode.Network => RetrieveFromNetwork(),
			_ => $"Nie wybrano trybu czytania dla {Name}"
		};
	}

	public virtual void SetChannelState(int channelNumber, bool state)
	{
		ChannelsEnable[channelNumber] = state;
	}
	protected abstract string RetrieveFromDriver();
	protected abstract string RetrieveFromNetwork();

	public virtual void Send(string message)
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
				break;
		}
	}

	protected abstract void SendByPipe(string message);
	protected abstract void SendByNetwork(string message);

	public abstract void ConvertValueToStandard();
	public abstract string ConvertValueToStandardString();

	public abstract void Close();
	
	public ErrorCode CreateSocket(string mode)
	{
		int port = mode == "Retrieve" ? retrievePort : sendPort;
		return CreateSocket(port);
	}
	
	public ErrorCode CreateSocket(int port)
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

	public void AddSignalChosen(string signal)
	{
		SignalsChosen.Add(signal);
		if (!Signals.ContainsKey(signal))
			Signals.Add(signal, new Dictionary<DateTime, double>());
		SignalsAvailable = SignalsAvailable.Where(s => s != signal).ToList();
		if (RetrieveData == RetrieveDataMode.Driver)
			SetSignals(Signals.Keys.ToList());
	}
	public void DeleteSignalChosen(string signal)
	{
		SignalsChosen.Remove(signal);
		Signals.Remove(signal);
		SignalsAvailable.Add(signal);
	}
}