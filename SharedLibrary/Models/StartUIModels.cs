using SMDataServiceProto.V1;

namespace SharedLibrary.Models
{
    public class SettingApp
    {
        //автосохранение отчета
        public bool? SaveReport { get; set; } = false;
        //звуковое оповещение
        public bool? SoundEnd { get; set; } = false;
        //только подключенные каналы
        public bool? ChannelConn { get; set; } = false;
        //запрашивать дооповещение
        public bool? ContinueNotify { get; set; } = false;

        //Подтверждение выбора сценария
        public bool? SitConfirm { get; set; } = true;
    }

    public class StartNotify
    {
        public OBJ_ID UnitID { get; set; } = new OBJ_ID();

        public List<OBJ_ID> ListOBJ { get; set; } = new List<OBJ_ID>();

        public List<string> SitNameList { get; set; } = new List<string>();

        public string SessId { get; set; } = "";

        public OBJ_ID? MsgId { get; set; }
    }
}
