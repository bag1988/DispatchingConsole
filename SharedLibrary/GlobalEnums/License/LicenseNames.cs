namespace SharedLibrary.GlobalEnums.License;

public static class LicenseNames
{
    public static string? GetNameByFutureId(LicenseFuture licenseFuture) => LicenseNameByLicenseFuture.TryGetValue(licenseFuture, out var value) ? value : null;

    private static readonly Dictionary<LicenseFuture, string> LicenseNameByLicenseFuture = new () {
        { LicenseFuture.UnspecifiedLicense, "Набор всех лицензий" }
      , { LicenseFuture.EDDS_Server, "Сервер ЕДДС" }
      , { LicenseFuture.EDDS_Client, "АРМ ОД ЕДДС" }
      , { LicenseFuture.OmegaM_VoIP, "Омега-М VoIP запись" }
      , { LicenseFuture.ASO_VoIP, "АСО VoIP оповещение" }
      , { LicenseFuture.ASO_SMSoGSM, "АСО SMS рассылка" }
      , { LicenseFuture.ASO_SMSoGATE, "АСО SMS рассылка с помощью сервера в локальной сети" }
      , { LicenseFuture.ASO_EMail, "АСО E-Mail" }
      , { LicenseFuture.ASOUZS_VoIP, "АСО-УЗС VoIP" }
      , { LicenseFuture.ASO_WSDL, "АСО WSDL" }
      , { LicenseFuture.KSEON_ARM_OD, "КСЭОН.Ядро.АРМ ОД" }
      , { LicenseFuture.KSEON_MONITORING, "КСЭОН.Ядро.Мониторинг" }
      , { LicenseFuture.KSEON_ARM_OD_MAP, "КСЭОН.АРМ ОД.Картография" }
      , { LicenseFuture.KSEON_MONITORING_SENSOR, "КСЭОН.Мониторинг СЕНСОР" }
      , { LicenseFuture.VideoIntercept, "Видеоперехват" }
      , { LicenseFuture.GSO_WS, "ПКО АС ОСОДУ. WS" }
      , { LicenseFuture.KSEON_LINK_EDDS, "КСЭОН.Модуль сопряжения ЕДДС" }
      , { LicenseFuture.SPPR_SENSOR, "СППР СЕНСОР.ЯДРО" }
      , { LicenseFuture.SPPR_SENSOR_LINK_MINUDO, "СППР СЕНСОР.МССМ 'МИНУДО'" }
      , { LicenseFuture.SPPR_SENSOR_LINK_TOXI_METEO, "СППР СЕНСОР.МСРК 'TOXI+METEO'" }
      , { LicenseFuture.SPPR_SENSOR_LINK_MSSOL_ALGORITHM, "СППР СЕНСОР.МССО-Л" }
      , { LicenseFuture._DUMMY_, "_DUMMY_" }
      , { LicenseFuture.USZ_Activation, "Генератор кодов активации УЗС" }
      , { LicenseFuture.GSO_CORE, "ПКО АС ОСОДУ. ЯДРО. WS" }
      , { LicenseFuture.GSO_IN_v6, "ПКО АС ОСОДУ. IN. v6" }
      , { LicenseFuture.GSO_OUT_v6, "ПКО АС ОСОДУ. OUT. v6" }
      , { LicenseFuture.GSO_IN_v5, "ПКО АС ОСОДУ. IN. v5" }
      , { LicenseFuture.GSO_OUT_v5, "ПКО АС ОСОДУ. OUT. v5" }
      , { LicenseFuture.GSO_IN_v3, "ПКО АС ОСОДУ. IN. v3" }
      , { LicenseFuture.GSO_OUT_v3, "ПКО АС ОСОДУ. OUT. v3" }
      , { LicenseFuture.KSEON_LIGA_AZOT, "КСЭОН. Модуль сопряжения с Лига АЗОТ" }
      , { LicenseFuture.TIMEOUT_KEY_LICENSE, "Таймаут лицензий без ключа" }
      , { LicenseFuture.SOFTWARE_LICENSE_GENERATOR, "Генератор лицензий" }
      , { LicenseFuture.PPSO, "ППСО" }
      , { LicenseFuture.GSO_WS_START, "ПКО АС ОСОДУ. WS START" }
      , { LicenseFuture.KSEON_WS, "КСЭОН. WS" }
      , { LicenseFuture.Omega_GSM, "Omega-GSM" }
      , { LicenseFuture.KSEON_SDIAV, "КСЭОН.МОНИТОРИНГ СДЯВ.СЕНСОР" }
      , { LicenseFuture.KSEON_MONITORING_FST_03B, "КСЭОН.МОНИТОРИНГ ФСТ-03В" }
      , { LicenseFuture.GSO_IN_v7, "ПКО АС ОСОДУ. IN. v7" }
      , { LicenseFuture.GSO_OUT_v7, "ПКО АС ОСОДУ. OUT. v7" }
      , { LicenseFuture.ASO_WSDL_SIT, "Aso WSDL Сценарии" }
      , { LicenseFuture.ENABLED_TSO, "Включение ТСО" }
      , { LicenseFuture.WHITE_IP_TECH, "ПКО АС ОСОДУ. Подключение технологических АРМ" }
      , { LicenseFuture.WHITE_IP, "ПКО АС ОСОДУ. Подключение \"боевых\" АРМ" }
      , { LicenseFuture.SensorUserKey, "Ключ доступа СЕНСОР" }

    };
}
