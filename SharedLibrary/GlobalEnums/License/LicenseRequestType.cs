namespace SharedLibrary.GlobalEnums.License;

public enum LicenseRequestType
{
    LoginRequestType = 0
  , CheckClientRequestType
  , CheckServerRequestType
  , LicenseFutureRequestType    // Запрос информации о лицензии
  , UpdateLicenseInfoType       // Нотификация об изменении какой-либо из лицензий
  , SubscribeFutureNotifyType   // Подписка на изменение конкретной лицензии
  , UnsubscribeFutureNotifyType // Отписка на изменение конкретной лицензии
  , FutureNotifyType            // Ответ на запрос или подписку состояния лицензии
  , SoftwareLicenseGenerateRequestType
  , UzsActivationGenerateRequestType
  , UnknownRequestType
}
