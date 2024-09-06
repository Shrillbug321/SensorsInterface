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
		DeviceMeasurementReadError,
		DeviceIsDisconnected,
		PipeNotConnected,
		SocketNotConnected,
		RetrieveDataModeNotSelected,
		SendDataModeNotSelected,
		SignalIsChosen,
		ApplicationIsDisconnected
	}

	public record MessageBoxElement
	{
		public string Text;
		public string Caption = "Błąd";
		public MessageBoxButton Button = MessageBoxButton.OK;
		public MessageBoxImage Image = MessageBoxImage.Error;
	}
	
	public static Dictionary<ErrorCode, MessageBoxElement> ErrorMessageBoxes = new()
	{
		{ErrorCode.Success, new MessageBoxElement {Text = "Sukces", Caption = "Wszystko OK", Image = MessageBoxImage.Information}},
		{ErrorCode.LibraryNotLoaded, new MessageBoxElement {Text = "Nie udało się wczytać biblioteki"}},
		{ErrorCode.DeviceSignalListIsTooLong, new MessageBoxElement {Text = "Lista sygnałów jest zbyt długa (dostarczono {0}, można {1})."}},
		{ErrorCode.DeviceNotOpen, new MessageBoxElement {Text = "Nie można otworzyć urządzenia."}},
		{ErrorCode.DeviceChannelsNotGet, new MessageBoxElement {Text = "Nie można pobrać liczby kanałów."}},
		{ErrorCode.DeviceChannelNotRun, new MessageBoxElement {Text = "Nie można pobrać uruchomić kanału."}},
		{ErrorCode.DeviceProfileNotSet, new MessageBoxElement {Text = "Nie można ustawić profilu."}},
		{ErrorCode.DeviceNotConnected, new MessageBoxElement {Text = "Nie można połączyć z urządzeniem"}},
		{ErrorCode.DeviceMeasurementCannotStart, new MessageBoxElement {Text = "Nie można rozpocząć pomiarów", Caption = "Błąd odczytu"}},
		{ErrorCode.DeviceMeasurementReadError, new MessageBoxElement {Text = "Nie można odczytać sygnału {0}. \nKontynuować?", Button = MessageBoxButton.YesNo}},
		{ErrorCode.DeviceIsDisconnected, new MessageBoxElement {Text = "Urządzenie {0} zostało rozłączone"}},
		{ErrorCode.PipeNotConnected, new MessageBoxElement {Text = "Nie można połączyć z potokiem.\nPrawdopodobnie aplikacja odbierająca nie jest włączona lub nie używa potoku."}},
		{ErrorCode.SocketNotConnected, new MessageBoxElement {Text = "Nie można połączyć z gniazdem.\nUpewnij się, że aplikacja odbierająca jest włączona oraz czy port nie jest zajęty."}},
		{ErrorCode.RetrieveDataModeNotSelected, new MessageBoxElement {Text = "Nie można uruchomić urządzenia, gdy nie wybrano sposobu pobierania danych."}},
		{ErrorCode.SendDataModeNotSelected, new MessageBoxElement {Text = "Nie wybrano sposobu wysyłania danych do aplikacji odbierającej.", Image = MessageBoxImage.Information}},
		{ErrorCode.SignalIsChosen, new MessageBoxElement {Text = "Sygnał {0} jest już wybrany.", Caption = "Próba duplikacji sygnału"}},
		{ErrorCode.ApplicationIsDisconnected, new MessageBoxElement {Text = "Aplikacja {0} przestała działać.", Caption = "Utracono połączenie z aplikacją"}},
	};

	public static MessageBoxResult ShowMessageBox(ErrorCode errorCode, string format="")
	{
		if (errorCode == ErrorCode.Success) return MessageBoxResult.OK;
		MessageBoxElement mbe = ErrorMessageBoxes[errorCode];
		if (mbe.Text.Contains('{'))
		{
			string[] split = format.Split(';');
			StringBuilder builder = new (mbe.Text);
			for (int i=0; i<split.Length; i++)
				builder.Replace($"{{{i}}}", split[i]);
			mbe.Text = builder.ToString();
		}
		return MessageBox.Show(mbe.Text, mbe.Caption, mbe.Button, mbe.Image);
	}
}