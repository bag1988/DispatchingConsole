namespace SharedLibrary.GlobalEnums.License;

public enum LicenseErrorCode
{
    ErrorSuccess = 0            // Ошибок нет
  , ErrorConnect = 1            // Ошибка подключение
  , ErrorLogin = 2              // Ошибка входа в систему
  , ErrorUnsupportedRequest = 3 // Не поддерживаемый запрос
  , ErrorInvalidArgument = 4    // Не корректные входные данные
  , ErrorInvalidAnswer = 5      // Не корректный ответ сервера
  , ErrorTimeoutAnswer = 6      // Таймаут ответа сервера. Сервер не ответил на запрос своевременно
  , ErrorLicenseNotPresent = 7  // Лицензия отсутствует
  , ErrorUnknown = 7            // Не известная ошибка
}
