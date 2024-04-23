using System.Runtime.InteropServices;

namespace SensorsInterface.Devices;

public unsafe partial class NeurobitOptima
{
	// PInvoke declaration for the native DLL exported function
	[DllImport("NeurobitDrv64.dll", CallingConvention = CallingConvention.StdCall)]
	public static extern int NdStartMeasurement(short DeviceContext, TStartMeasurement.MeasurementMode Mode);

	[DllImport("NeurobitDrv64.dll", CallingConvention = CallingConvention.StdCall)]
	public static extern int NdOpenDevContext(string deviceName);

	[DllImport("NeurobitDrv64.dll", CallingConvention = CallingConvention.StdCall)]
	public static extern int NdSetDevConfig(int deviceContext, char[] buf, int size);

	[DllImport("NeurobitDrv64.dll", CallingConvention = CallingConvention.StdCall)]
	public static extern int NdGetParam(short parameter, int channel, NDGETVAL* value);

	[DllImport("NeurobitDrv64.dll", CallingConvention = CallingConvention.StdCall)]
	public static extern int NdSetParam(short parameter, int channel, NDSETVAL* value);

	[DllImport("NeurobitDrv64.dll", CallingConvention = CallingConvention.StdCall)]
	public static extern int NdStr2Param([MarshalAs(UnmanagedType.LPStr)] string s, short parameterId, short space);

	[DllImport("NeurobitDrv64.dll", CallingConvention = CallingConvention.StdCall)]
	public static extern NDPARAM* NdParamInfo(short par, short chan);

	/* Maximum number of parameters */
	private const uint ND_MAX_PARAMS = 0x100u;

	/* Offset of numeric id. space for channel parameters */
	private const uint ND_PAR_CH = ND_MAX_PARAMS / 2;

/* Max. number of output measured signals handled in the application */
	private const int MAX_SIGNALS = 16;

	public struct DevContextInfo
	{
		/* Device context */
		public int deviceContext;

		/* Device model name */
		[MarshalAs(UnmanagedType.LPStr)] public string model;

		/* Number of channels */
		public int dev_chans;

		/* Array of sample scaling factors */
		public fixed float coeff[MAX_SIGNALS];

		/* Channel names to print */
		[MarshalAs(UnmanagedType.LPStr)]
		public string names;
	}

	public struct NDGETVAL
	{
		/* Option id specified for ND_T_LIST */
		public uint opt;

		/* Value specified when ND_T_LIST is not set */
		public NDVAL val;

		/* Value type */
		public byte type;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct NDVAL
	{
		[FieldOffset(0)] public int i;
		[FieldOffset(0)] public bool b;
		[FieldOffset(0)] public float f;

		[FieldOffset(0)] [MarshalAs(UnmanagedType.LPStr)]
		public string t;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct NDSETVAL
	{
		[FieldOffset(0)] public uint opt;
		[FieldOffset(0)] public NDVAL val;
	}

	/* Option of a list */
	public struct NDOPT
	{
		/* Numerical identifier of the option.
			Should remain the same in all software versions. */
		public uint id;

		/* Option value */
		public NDVAL val;

		/* Help for the option (or NULL) */
		[MarshalAs(UnmanagedType.LPStr)] public string help;
	};

	/* Range of ND_T_INT parameter (without ND_T_LIST) */
	public struct NdIntRange
	{
		public int min;
		public int max;
	};

/* Range of ND_T_FLOAT parameter (without ND_T_LIST) */
	public struct NdFloatRange
	{
		public float min;
		public float max;
	};

	/* Parameter domain */
	[StructLayout(LayoutKind.Explicit)]
	public struct NDDOMAIN
	{
		/* Maximum text length [characters] (without ND_T_LIST) */
		[FieldOffset(0)] public uint tlen;

		/* Lists for ND_T_LIST */
		[FieldOffset(0)] public NDOPT* opts;

		/* For parameters without ND_T_LIST: */
		/* Trimmed range for ND_T_INT parameter */
		[FieldOffset(0)] public NdIntRange irange;

		/* Trimmed range for ND_T_FLOAT parameter */
		[FieldOffset(0)] public NdFloatRange frange;
	};

/* Parameter description structure.
	It collects all parameters of multi-channel data acquisition/output unit.
	Parameters can be constant or variable,	including dependencies on other
	parameters. */
	public struct NDPARAM
	{
		/* Numeric identifier of the parameter */
		public short id;

		/* Name of parameter (for rendering) */
		[MarshalAs(UnmanagedType.LPStr)] public string name;

		/* Name of group of parameters (or NULL); for possible rendering only. */
		[MarshalAs(UnmanagedType.LPStr)] public string group;

		/* Value type */
		public byte type;

		/* Access flags */
		public byte flags;

		/* Parameter domain */
		public NDDOMAIN domain;

		/* Physical unit of the parameter (or NULL) */
		[MarshalAs(UnmanagedType.LPStr)] public string unit;

		/* Default value */
		public NDSETVAL def;

		/* Array of parameters depending on this parameter (or NULL) */
		public short* dep;

		/* Help for the parameter (or NULL) */
		[MarshalAs(UnmanagedType.LPStr)] public string help;
	}

	public enum ParameterIds
	{
		/* General parameters */
		ND_PAR_LINK = 0,

		/**Link type / LOCAL */
		ND_PAR_CHAN_NUM,

		/**Number of channels */
		ND_PAR_IND_SUPPORT,

		/**Support of indicators */
		ND_PAR_ADDR, /* Device address / LOCAL */
		ND_PAR_POW_FREQ, /* Power mains frequency */
		ND_PAR_SILENT, /* Silent mode */
		/* <- new general parameters can be added here */

		ND_GEN_PAR_NUM, /* Have to be at the end of general parameter list */

		/* Channel parameters */
		ND_PAR_CH_NAME = (int)ND_PAR_CH,

		/**Channel name */
		ND_PAR_CH_EN,

		/**Channel enable (appears in all multi-channel devices) */
		ND_PAR_CH_DIR,

		/**Channel direction: 0-"Input", 1-"Output" */
		ND_PAR_CH_RESOL,

		/**ADC or DCA resolution */
		ND_PAR_CH_LABEL,

		/**Channel label / may be LOCAL */
		ND_PAR_CH_TRANSDUCER,

		/**Sensor or transducer info / may be LOCAL */
		ND_PAR_CH_PROF, /* Channel profile (aggregates several settings for some types of signals) */
		ND_PAR_CH_RANGE_MAX,

		/**Measurement range maximum */
		ND_PAR_CH_RANGE_MIN,

		/**Measurement range minimum */
		ND_PAR_CH_SR,

		/**Sample rate */
		ND_PAR_CH_BAND_FL,

		/**Absolute lower limit of frequency band */
		ND_PAR_CH_BAND_FU,

		/**Relative upper limit of frequency band */
		ND_PAR_CH_FUNC, /* Channel function */
		ND_PAR_CH_CHAR, /* Frequency characteristic */
		ND_PAR_CH_POW_FT, /* Power interference filter */
		ND_PAR_CH_SUM_DISC, /* Sum disconnected */
		ND_PAR_CH_TEST_SR, /* Sampling rate for input circuit continuity test */
		ND_PAR_CH_TEST_RANGE, /* Measurement range for input circuit continuity test */
		ND_PAR_CH_REF, /* Connection of "-" input to common reference electrode */

		/* Added for new NO architecture: */
		ND_PAR_CH_CAP_CON, /* Channel connection to EEG cap */
		/* <- new channel parameters can be added here */

		ND_CHAN_PAR_END /* Have to be at the end of general parameter list */
	}
}