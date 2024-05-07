using System.Text;
using System.Windows;

namespace SensorsInterface.Helpers;

public static class Error
{
	public enum ErrorCode
	{
		Success,
		LibraryNotLoaded,
		DeviceNotOpen,
		DeviceSignalListIsTooLong,
		DeviceChannelsNotGet,
		DeviceChannelNotRun,
		DeviceProfileNotSet,
		DeviceNotConnected,
		DeviceMeasurementCannotStart,
		DeviceMeasurementReadError
	}

	public record MessageBoxElement
	{
		public string Text;
		public MessageBoxButton Button = MessageBoxButton.OK;
		public MessageBoxImage Image = MessageBoxImage.Error;
	}
	
	public static Dictionary<ErrorCode, MessageBoxElement> ErrorMessageBoxes = new()
	{
		{ErrorCode.Success, new MessageBoxElement {Text = "Sukces", Image = MessageBoxImage.Information}},
		{ErrorCode.LibraryNotLoaded, new MessageBoxElement {Text = "Nie udało się wczytać biblioteki"}},
		{ErrorCode.DeviceSignalListIsTooLong, new MessageBoxElement {Text = "Lista sygnałów jest zbyt długa (dostarczono {0}, można {1})."}},
		{ErrorCode.DeviceNotOpen, new MessageBoxElement {Text = "Nie można otworzyć urządzenia."}},
		{ErrorCode.DeviceChannelsNotGet, new MessageBoxElement {Text = "Nie można pobrać liczby kanałów."}},
		{ErrorCode.DeviceChannelNotRun, new MessageBoxElement {Text = "Nie można pobrać uruchomić kanału."}},
		{ErrorCode.DeviceProfileNotSet, new MessageBoxElement {Text = "Nie można ustawić profilu."}},
		{ErrorCode.DeviceNotConnected, new MessageBoxElement {Text = "Nie można połączyć z urządzeniem"}},
		{ErrorCode.DeviceMeasurementCannotStart, new MessageBoxElement {Text = "Nie można rozpocząć pomiarów"}},
		{ErrorCode.DeviceMeasurementReadError, new MessageBoxElement {Text = "Nie można odczytać sygnału"}},
	};

	public static void ShowMessageBox(ErrorCode errorCode, string caption="", string format="")
	{
		MessageBoxElement mbe = ErrorMessageBoxes[errorCode];
		if (mbe.Text.Contains('{'))
		{
			string[] split = format.Split(';');
			StringBuilder builder = new (mbe.Text);
			for (int i=0; i<split.Length; i++)
			{
				builder.Replace($"{{{i}}}", split[i]);
			}
			mbe.Text = builder.ToString();
		}
		MessageBox.Show(mbe.Text, caption, mbe.Button, mbe.Image);
	}
}