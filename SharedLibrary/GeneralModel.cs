using AsoDataProto.V1;
using SMSSGsoProto.V1;
using SMDataServiceProto.V1;

namespace SharedLibrary
{
    public class SetFontModel
    {
        public OBJ_ID? Obj { get; set; }
        public byte[]? Font { get; set; }
    }


    public class UpdateList
    {
        public CListInfo? Info { get; set; }
        public List<CListItemInfo>? Lists { get; set; }
    }


    public class DictionryList
    {
        public static Dictionary<int, string> AsoTypeConnectList
        {
            get
            {
                return new Dictionary<int, string> {
                  { 0x04,   "LPT (K3, K4, K5)" },//ASO_TC_LPT
                  { 0x02,   "HSCOM (K6)" },//ASO_TC_COM
                  { 0x08,   "HSCOM (K43)" },//ASO_TC_USB
                  { 0x06,  "SMTP" },//ASO_TC_SMTP
                  //{ 0x07, TEXT("SMS Gate") },//ASO_TC_SMSGATE
                  { 0x0A,  "GSM Device" },//ASO_TC_GSMT
                  //{ 0x09,  TEXT("SNMP") },//ASO_TC_SNMP
                  { 0x0B,  "VoIP" },//ASO_TC_VOIP
                  { 0x0C,"ASO GSM (SIMCOM)" },//ASO_TC_ASOGSM
                  { 0x0D,  "DCOM" },//ASO_TC_DCOM
                  { 0x0E,  "WSDL" }//ASO_TC_WSDL
                };
            }
        }
    }

    public class ChangePassword
    {
        public string? EncryptPassword { get; set; }
        public string? OldPassword { get; set; }
        public string? NewPassword { get; set; }
    }

    public class ParamControllerModel
    {
        public int DwCommand { get; set; } = 0;       // код команды или параметра настройки контроллера АСО
        public string? SzNameParam { get; set; } = ""; // наименование параметра

        public int DwParam { get; set; } = 100;        // величина параметра в дискретах
        public bool BEnable { get; set; } = false;        // для линии связи - разрешена к использованию или нет (используется для ComboBox)
        public int DwRatio { get; set; } = 10;          // дискрет величины (10 мс, 1 раз, 1 мс и т.д.)
                                                        // если dwRatio = 0, то эта структура используется для задания параметров ComboBox
        public int DwDevide { get; set; } = 1000;         // множитель единицы измерения (1000 от "сек" - 1 мсек и т.д.)
        public string? SzDim { get; set; } = "";         // единицы измерения ("с" - сек, "мс" - мсек, "" - разы)
        public int DwRealParam { get; set; } = 0;      // реальная величина параметра в дискретах (заполняется из базы)
    };


    public class SetControllingDevice
    {
        public UpdatingControllingDeviceFlags flags { get; set; } = new();
        public AsoControllingDeviceInfo ControllingDevice { get; set; } = new();
        public List<AsoCommonProto.V1.ControllerDescription> ControllDesc { get; set; } = new();
    }
}
