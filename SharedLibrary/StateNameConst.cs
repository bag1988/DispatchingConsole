namespace SharedLibrary
{
    public struct StateNameConst
    {
        public const string VapidDetails = "vapiddetails";
        //public const string PushMessage = "pushmessage";
        public const string PushSetting = "pushsetting";

        public const string StateStore = "statestore";
        public const string ChannelContainer = "channelcontainer";
        public const string RecordSetting = "recordsetting";
        public const string Authorize = "authorize";

        //подсистемы
        public const string SubsystemArray = "subsystemarray";

        //открытые сообщения
        public const string ReadCmd = "readcmd";

        //список записываемых фонограмм
        public const string WriteCmd = "writecmd";

        //Счетчик для части ручного сообщения
        public const string NumberPart = "numberpart";

        //Сессия для регистрации ПУ
        public const string SessRegCU = "sessregcu";

        //OpenCmd m_OpenCmd
        public const string OpenCmd = "opencmd"; // <CSession *, dwReleaseTickCount>

        //КЭШ состояния задач по расписанию
        public const string TasksSchedule = "tasksschedule"; 
    }
}
