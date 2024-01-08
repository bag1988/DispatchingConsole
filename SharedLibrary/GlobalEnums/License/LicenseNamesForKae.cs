namespace SharedLibrary.GlobalEnums.License;

public static class LicenseNamesForKae
{
    public static string? GetNameByFutureId(LicenseFuture licenseFuture) => LicenseNameByLicenseFutureForKae.TryGetValue(licenseFuture, out var value) ? value : null;

    private static readonly Dictionary<LicenseFuture, string> LicenseNameByLicenseFutureForKae = new() {
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
      , { LicenseFuture.KSEON_ARM_OD, "АРМ оповещения ПК ОГЗ \"Уран\"" }
      , { LicenseFuture.KSEON_MONITORING, "ПК ОГЗ \"Уран\".Мониторинг" }
      , { LicenseFuture.KSEON_ARM_OD_MAP, "ПК ОГЗ \"Уран\".ПМ МК" }
      , { LicenseFuture.KSEON_MONITORING_SENSOR, "ПК ОГЗ \"Уран\".Мониторинг Датчиков" }
      , { LicenseFuture.VideoIntercept, "Видеоперехват" }
      , { LicenseFuture.GSO_WS, "ПК ОГЗ \"Уран\".WS" }
      , { LicenseFuture.KSEON_LINK_EDDS, "ПК ОГЗ \"Уран\".Модуль сопряжения ЕДДС" }
      , { LicenseFuture.SPPR_SENSOR, "ПК ОГЗ \"Уран\".Ядро СППР" }
      , { LicenseFuture.SPPR_SENSOR_LINK_MINUDO, "ПК ОГЗ \"Уран\".МССМ 'МИНУДО' СППР" }
      , { LicenseFuture.SPPR_SENSOR_LINK_TOXI_METEO, "ПК ОГЗ \"Уран\".МСРК 'TOXI+METEO' СППР" }
      , { LicenseFuture.SPPR_SENSOR_LINK_MSSOL_ALGORITHM, "ПК ОГЗ \"Уран\".МССО" }
      , { LicenseFuture._DUMMY_, "_DUMMY_" }
      , { LicenseFuture.USZ_Activation, "Генератор кодов активации УЗС" }
      , { LicenseFuture.GSO_CORE, "ПК ОГЗ \"Уран\". ЯДРО. WS" }
      , { LicenseFuture.GSO_IN_v6, "ПК ОГЗ \"Уран\". IN. v6" }
      , { LicenseFuture.GSO_OUT_v6, "ПК ОГЗ \"Уран\". OUT. v6" }
      , { LicenseFuture.GSO_IN_v5, "ПК ОГЗ \"Уран\". IN. v5" }
      , { LicenseFuture.GSO_OUT_v5, "ПК ОГЗ \"Уран\". OUT. v5" }
      , { LicenseFuture.GSO_IN_v3, "ПК ОГЗ \"Уран\". IN. v3" }
      , { LicenseFuture.GSO_OUT_v3, "ПК ОГЗ \"Уран\". OUT. v3" }
      , { LicenseFuture.KSEON_LIGA_AZOT, "ПК ОГЗ \"Уран\".Модуль сопряжения с Лига АЗОТ" }
      , { LicenseFuture.TIMEOUT_KEY_LICENSE, "Таймаут лицензий без ключа" }
      , { LicenseFuture.SOFTWARE_LICENSE_GENERATOR, "Генератор лицензий" }
      , { LicenseFuture.PPSO, "ППСО" }
      , { LicenseFuture.GSO_WS_START, "ПК ОГЗ \"Уран\".WS START" }
      , { LicenseFuture.KSEON_WS,  "ПК ОГЗ \"Уран\".ПМ МК WS" }
      , { LicenseFuture.Omega_GSM, "Omega-GSM" }
      , { LicenseFuture.KSEON_SDIAV, "ПК ОГЗ \"Уран\".МОНИТОРИНГ СДЯВ" }
      , { LicenseFuture.KSEON_MONITORING_FST_03B, "ПК ОГЗ \"Уран\".МОНИТОРИНГ ФСТ-03В" }
      , { LicenseFuture.GSO_IN_v7, "ПК ОГЗ \"Уран\".IN. v7" }
      , { LicenseFuture.GSO_OUT_v7, "ПК ОГЗ \"Уран\".OUT. v7" }
      , { LicenseFuture.ASO_WSDL_SIT, "Aso WSDL Сценарии" }
      , { LicenseFuture.ENABLED_TSO, "Включение ТСО" }
      , { LicenseFuture.WHITE_IP_TECH, "ПК ОГЗ \"Уран\".Подключение технологических АРМ" }
      , { LicenseFuture.WHITE_IP, "ПК ОГЗ \"Уран\".Подключение \"боевых\" АРМ" }
      , { LicenseFuture.SensorUserKey, "Ключ индивидуального доступа" }
    };
}
