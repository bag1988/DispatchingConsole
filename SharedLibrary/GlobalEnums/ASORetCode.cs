namespace SharedLibrary.GlobalEnums
{
    //-------------------------------------------------
    //  Описатели кодов ответа от контроллера АСО и логики управления
    //-------------------------------------------------
    public enum ASORetCode : uint
    {
        ASO_RC_UNDEFINED_ANSWER = 0x00,    // неизвестный ответ
        ASO_RC_ANSWER_TICKER = 0x01,       // ответ с подтверждением тикером
        ASO_RC_ANSWER = 0x02,              // ответ без подтверждения
        ASO_RC_NOANSWER = 0x03,            // абонент не ответил
        ASO_RC_BUSY = 0x04,                // абонент занят
        ASO_RC_NOTREADYLINE = 0x05,        // нет тона в линии
        ASO_RC_BUSYCHANNEL = 0x06,         // канал занят
        ASO_RC_READY = 0x07,               // канал свободен и готов к работе
        ASO_RC_HANDSET_REMOVED = 0x08,     // трубка снята абонентом
        ASO_RC_ANSWER_DTMF = 0x09,         // получен DTMF ответ
        ASO_RC_ERROR_ATS = 0x0A,           // ошибка соединения (нет тона после набора номера)
        ASO_RC_INTER_ERROR = 0x0B,         // внутренняя ошибка контроллера (АСО-М)
        ASO_RC_BREAK_ANSWER = 0x0C,        // рано положена трубка (сообщение не дослушано до конца)
        ASO_RC_ANSWER_SETUP = 0x0D,        // параметр принят / подтверждение приема цифры
        ASO_RC_ANSWER_FAX = 0x0E,          // ответил факс
        ASO_RC_NOTCONTROLLER = 0x0F,       // контроллер отсутствует или нет связи с блоком АСО
        ASO_RC_ANSWER_COMMIT = 0x10,       // Ответ с подтверждением (DCOM)
        ASO_RC_ABON_DISCLAIM = 0x11,       // Абонент отказался от соединения
        ASO_RC_LINE_CONNECT = 0x17,        // трубка снята (АСО)
        ASO_RC_NOFREECHANNEL = 0x80000020, // нет свободных каналов
        ASO_RC_NOCHANNELS = 0x80000021,    // нет каналов
        ASO_RC_NOLOADNEWMESSAGE = 0x80000022, // невозможно загрузить новое звуковое сообщение в связи с неоконченными оповещениями с предыдущим сообщением
        ASO_RC_ERRORLOADMESSAGE = 0x80000023, // ошибка загрузки звукового сообщения
        ASO_RC_ERRORSETCONNECT = 0x80000024, // ошибка соединения с абонентом
        ASO_RC_NOFREECHANNELGLOBALMASK = 0x80000025, // нет свободных каналов с глобальной маской
        ASO_RC_NOFREECHANNELUSERMASK = 0x80000026, // нет свободных каналов с пользовательской маской
        ASO_RC_NOMESSAGE = 0x80000027, // нет сообщения (сообщение с ID = 0)
        // Доставка сообщения через промежуточный сервер
        ASO_RC_ERROR_SERVER_CONNECT = 0x80000100,  // Ошибка соединения с сервером
        ASO_RC_ERROR_SERVER_LOGIN = 0x80000101,    // Ошибка на сервере
        ASO_RC_ERROR_SERVER_DELIVERY = 0x80000102, // Ошибка доставки сообщения
        // коды возврата AsoInit
        ASO_RC_INIT_ERR_SOUND_DEVICE = 0x800FF735, // ошибка инициализации звукового устройства
        ASO_RC_INIT_NULL_DATA = 0x800FF736, // отсутствуют некоторые из входных данных, описывающих блок АСО
        ASO_RC_INIT_LPT_DISABLE = 0x800FF737, // LPT устройство в системе отсутствует
        ASO_RC_INIT_ASODRV_DISABLE = 0x800FF738, // драйвер AsoDrv для заданного порта отсутствует
        ASO_RC_INIT_LPT_BUSY = 0x800FF739, // LPT порт занят другим устройством
        ASO_RC_INIT_INNER_ERROR = 0x800FF73A, // внутренняя ошибка ЛУ
        ASO_RC_INIT_UNKNOWN_CONN_TYPE = 0x800FF73B, // неизвестный тип устройства
        ASO_RC_INIT_UNKNOWN_CONTRL_TYPE = 0x800FF73C, // неизвестный тип контроллера
        ASO_RC_INIT_ERR_MAINTHREAD_ID = 0x800FF73D, // некорректный ID главного потока ЛО
        ASO_RC_INIT_CONTRL_MISMATCH = 0x800FF73E, // ошибка сопоставления контроллера и линии
        ASO_RC_INIT_DEVICE_MISMATH = 0x800FF73F, // ошибка сопоставления устройства и линии
        ASO_RC_INIT_HWCONNECT_ERROR = 0x800FF740, // нет связи с блоком
        ASO_RC_INIT_PROTECT_FAILURE = 0x800FF741, // Ошибка проверки защиты (HASP KEY NOT PRESENT OR INVALID KEY)
        ASO_RC_INIT_SUCCESS = 0x00000000, // Успешная инициализация ЛУ
        ASO_RC_INIT_ALREADY = 0x00000001, // Успешная инициализация ЛУ была ранее произведена
        NumASORetCode = 0x00000002,
    }
}
