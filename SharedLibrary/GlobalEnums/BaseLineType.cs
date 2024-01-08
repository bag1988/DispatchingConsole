namespace SharedLibrary.GlobalEnums
{
    //Базовый тип линии (новая редакция)
    public enum BaseLineType
    {
        LINE_TYPE_UNDEF        // неопределен
      , LINE_TYPE_DIAL_UP      // коммутируемая телефонная линия связи
      , LINE_TYPE_DEDICATED    // выделенная линия связи
      , LINE_TYPE_PAGE         // пейджерное оповещение (АСО)
      , LINE_TYPE_PHISICAL = 3 // выделенная физическая линия связи (СЗС)
      , LINE_TYPE_RADIO        // радиоканал
      , LINE_TYPE_HSCOM        // Высокоскоростной СОМ
      , LINE_TYPE_SMTP         // SMTP протокол Ethernet
      , LINE_TYPE_GSM_TERMINAL // GSM терминал на COM порту
      , LINE_TYPE_SNMP_GATE = 9    // сетевой порт провайдера моильной связи для передачи SMS сообщений
      , LINE_TYPE_DCOM         // DCOM клиент
      , LINE_TYPE_WSDL         // WSDL веб-сервис
    }
}
