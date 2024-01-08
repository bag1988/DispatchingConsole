namespace SharedLibrary
{
    public struct SqlTableValue
    {
        public const int SubsystemName = 0;//SubsystemID
        public const int SZSDeviceType = 20;//SZSDevType
        public const int AsoAbonentStatusName = 100;//100+ID_status++++++++++
        public const int AsoConnectionType = 200;//200+ConnTypeID++++++++
        public const int AsoDepartment = 300;//300+ID_dep+++++++++
        public const int Command = 400;//CmdID = 1 and SubsystemID = 4 (SubsystemID==400+CmdID else 410+CmdID------------------
        public const int ControlUnit = 500;//500+UnitID+++++++++++++
        public const int DeviceStatus = 600;//600+Status---------------
        public const int GsoUserSecurityType = 700;//TypeID-------------
        public const int LineRestrict = 800;//8{BitNumber+RestrictType}{LineType+RestrictType}+++++++++++
        public const int LineTypes = 900;//900+LineType+++++++++++++++++++
        public const int ProcessCommandStatus = 1000;//1000+StatusID-----------------
        public const int ProcessCommandType = 1100;//1100+CommandTypeID---------------
        public const int Report = 1200;//1200+RepID+++++++++++++++
        public const int SessionStatusNames = 1300;//1300+Stat_sess+++++++++++++++
        public const int Situation = 1400;//1400+SitID +-+-+-+-+-+-+-+-+
        public const int SituationType = 1500;//1500+SitTypeID++++++++++++
        public const int StaffStatus = 1600;//1600+StatusID------------------
        public const int StartSituationStatus = 1700;//1700+StatusID----------------
        public const int SZSLineType = 1800;//1800+LineType----------------------
        public const int SZSSensor = 1900;//1900+SensorType-----------------------
        public const int ReportItem = 2000;//2000+(100*repId)+Columnid++++++++++++++
        public const int ReportItemContr = 3000;//3000+(100*repId)+Columnid+++++++++++
        public const int SessionItemStatus = 4000;//4000+{SubsystemID.ToString()+Status.ToString()}
        public const int SessionItemStatusResult = 5000;//5000+{SubsystemID.ToString()+Status.ToString()}
    }
}
