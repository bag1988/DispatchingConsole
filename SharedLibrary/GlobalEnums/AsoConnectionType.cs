namespace SharedLibrary.GlobalEnums
{
    public enum AsoConnectionType
    {
        Unspecified
      , Synthesis = 0x01
      , Com = 0x02       // CLAsoHSCOM.dll
      , Lpt = 0x04       // CLAsoLPT.dll
      , Usb = 0x08       // CLAso43.dll
      , Smtp = 0x06      // CLAsoSMTP.dll
      , SmsGate = 0x07   // CLAsoSMSGate.dll
      , Snmp = 0x09      //
      , GsmT = 0x0A      // CLAsoGSMT.dll
      , VoIp = 0x0B      // CLAsoVoIP.dll
      , Gsm = 0x0C       // CLAsoGSM.dll
      , DCom = 0x0D      // CLAsoDCOM.dll
      , Wsdl = 0x0E      // CLAsoWSDL.dll
    }
}
