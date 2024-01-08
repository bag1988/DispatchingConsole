namespace SharedLibrary.Models
{
    public class ParamsSystem
    {
        public string? AllowedUsbStorage { get; set; }//Разрешенный USB накопитель
        public string? TotalStopByNewNotify { get; set; }//Прерывание текущего оповещения при получении команды
        public string? IsCheckCu { get; set; }//Подконтрольные ПУ
        public string? IsPOSIgnore { get; set; }//Игнорировать ПОС
        public string? IntervalUpdateLicense { get; set; }//Интервал обновления License
        public string? IntervalUpdateState { get; set; }//Интервал обновления ViewSate
        public string? IsHistoryCommand { get; set; }//Протокол обмена
        public string? LocalIpAddress { get; set; }//Адрес локального ПУ
        public string? ARMPath { get; set; }//Путь к звуковым фонограммам, записанных при оповещении в АРМ руководителя
        public string? Message_Log { get; set; }//Путь к звуковым фонограммам, записанным при передаче между ПУ
        public string? MinTimeBetweenSupportNotify { get; set; }//Минимальное время между оповещениями обслуживающего персонала об авариях
        public string? NotifyStaff { get; set; }//Показывать окно информационных сообщений
        public string? NotifyStaffDomain { get; set; }//Имя домена
        public string? NotifyStaffName { get; set; }//Имя ПЭВМ (IP адрес) для вывода окна информационных сообщений
        public string? NotifyStaffPassword { get; set; }//Пароль пользователя ПЭВМ
        public string? NotifyStaffUserName { get; set; }//Имя пользователя ОС для доступа к ПЭВМ
        public string? P16xFolder { get; set; }//Путь к звуковым фонограммам, записанных при оповещении П16х
        public string? POWEROFF_ON_CRITICAL_ERROR { get; set; }//Выключение питания ПЭВМ при критических сбоях
        public string? RESTART_ON_CRITICAL_ERROR { get; set; }//Перезагрузка ПЭВМ при критических сбоях
        public string? ServerReporterAddress { get; set; }//Параметр подключения сервера отчетов.
        public string? SitSupportNotify { get; set; }//Сценарий для оповещения обслуживающего персонала при ошиках контроля состояния аппаратуры
        public uint STORAGE_CHECKDEVICE_PERIOD { get; set; }//Период оперативного хранения контроля состояния устройств
        public uint STORAGE_EVENTLOG_PERIOD { get; set; }//Период оперативного хранения журнала событий
        public uint STORAGE_MSG_ARM_PERIOD { get; set; }//Период оперативного хранения сообщений АРМ (руководителя или среднего звена)
        public uint STORAGE_MSG_CU_PERIOD { get; set; }//Период оперативного хранения сообщений передаваемых между ПУ
        public uint STORAGE_MSG_P16x_PERIOD { get; set; }//Период оперативного хранения сообщений приемника П16х
        public uint STORAGE_MSG_TTS_PERIOD { get; set; }//Период оперативного хранения сообщений синтеза речи
        public uint STORAGE_NOTIFYHISTORY_PERIOD { get; set; }//Период оперативного хранения истории оповещения
        public string? TTSEngine { get; set; }//Используемый синезатор речи
        public string? TTSVoice { get; set; }//Голос(мужской, женский)
        public string? TTSFREQUENCY { get; set; }//Частота преобразования текста в речь(качество).
        public string? TTSMessagePath { get; set; }//Путь к звуковым фонограммам, сформированных в результате синтеза речи.
        public string? PathAsoRecord { get; set; }//Каталог записи ответов
        public string? PathFonogramm { get; set; }//Каталог фонограмм

        public string? PathSaveMsg { get; set; }//Каталог сохранения сообщений.

        public uint SaveDay { get; set; }//Сохранять запись ответа(фонограммы) не более, суток

        public string? ControlUnitMode { get; set; }//Режим работы локального ПУ

        public string? UZSMonitorTimeOutDetect { get; set; }//Сигнализация при ошибках "Нет связи с УЗС"
        public string? UZSMonitorErrorDetect { get; set; }//Сигнализация при ошибках тестирования УЗС

        public string? SERVER_LIST_4_CHECK_NETWORK { get; set; }//Список серверов, пропадание связи с которыми является критическим сбоем

        public string? ReporterMailRecipient { get; set; }//Параметр адрес отправителя отчётов.



    }
}
