namespace SharedLibrary.GlobalEnums
{
    public static class ChannelDispatcher
    {
        public const long ERR_NOCHANNELS = 0x80000201L;
        public const long ERR_NOFREECHANNELS = 0x80000202L;
        public const long ERR_CHANNELISLOCKED = 0x80000203L;
        public const long WARN_CHANNELNOTLOCKED = 0x40000204L;
        public const long ERR_CHANNELISBUSY = 0x80000205L;
        public const long WARN_CHANNELNOTBUSY = 0x40000206L;
        public const long ERR_CHANNELDISABLED = 0x80000207L;  // duplicate of ERR_UNITDISABLED
        public const long WARN_CHANNELDISABLED = 0x40000207L; // duplicate of WARN_UNITDISABLED
        public const long ERR_CHANNELNOTCATCHED = 0x80000208L;
        // generate by SCSCmd
        public const long ERR_CHANNELWASLOST = 0x80000209L;
        public const long ERR_CONNECTIONWASLOST = 0x8000020AL;
        public const long ERR_BADDATACRC = 0x8000020BL;
        public const long ERR_CANTTRANSMITDATA = 0x8000020CL;
        public const long ERR_CANTRECEIVEDATA = 0x8000020DL;
    }
}
