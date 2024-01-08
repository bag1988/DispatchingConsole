namespace SharedLibrary.GlobalEnums.License;

public enum LicenseFuture
{
    UnspecifiedLicense
  , EDDS_Server
  , EDDS_Client
  , OmegaM_VoIP
  , ASO_VoIP
  , ASO_SMSoGSM
  , ASO_SMSoGATE
  , ASO_EMail
  , ASOUZS_VoIP
  , ASO_WSDL
  , KSEON_ARM_OD
  , KSEON_MONITORING
  , KSEON_ARM_OD_MAP
  , KSEON_MONITORING_SENSOR
  , VideoIntercept
  , GSO_WS
  , KSEON_LINK_EDDS
  , SPPR_SENSOR
  , SPPR_SENSOR_LINK_MINUDO
  , SPPR_SENSOR_LINK_TOXI_METEO
  , SPPR_SENSOR_LINK_MSSOL_ALGORITHM
  , _DUMMY_
  , USZ_Activation
  , GSO_CORE
  , GSO_IN_v6
  , GSO_OUT_v6
  , GSO_IN_v5
  , GSO_OUT_v5
  , GSO_IN_v3
  , GSO_OUT_v3
  , KSEON_LIGA_AZOT
  , TIMEOUT_KEY_LICENSE
  , SOFTWARE_LICENSE_GENERATOR
  , PPSO
  , GSO_WS_START
  , KSEON_WS
  , Omega_GSM
  , KSEON_SDIAV
  , KSEON_MONITORING_FST_03B
  , GSO_IN_v7
  , GSO_OUT_v7 = 40
  , ASO_WSDL_SIT
  , ENABLED_TSO
  , WHITE_IP_TECH
  , WHITE_IP
  , UnknownFuture // До сих идёт чтение лицензий из HASP ключей, not used, just the last one
  , SensorUserKey = 2001
}
