namespace SharedLibrary.Models
{
    public class ReadFileRequest
    {
        public string FileName { get; set; } = string.Empty;
        public int CodePage { get; set; }
        public int IgnoreStrFirstCount { get; set; }
        public string Separotor { get; set; } = ";│";
        public string? ContentType { get; set; }
        public string? SelectSheet { get; set; }
        public int NumberPage { get; set; } = 0;
        public int DataSize { get; set; } = 100;
        public bool FirstStringAsName { get; set; } = false;
    }

    public class ReadFileRequestInfo
    {
        public ReadFileRequest FileRequest { get; set; } = new();
        public InfoAbonColumn ColumnInfo { get; set; } = new();
        //public bool AllData { get; set; } = false;
        public bool ContractNumber { get; set; } = false;
        public bool CurrencyCode { get; set; } = false;
        public bool WaitingTone { get; set; } = false;
        public bool RoundUp { get; set; } = false;
        public bool AccountDebt { get; set; } = false;
        public int NumberPage { get; set; } = 0;
        public bool PhoneLine { get; set; } = false;
        public bool SendSms { get; set; } = false;
        public bool TimeLimit { get; set; } = false;
        public TimeOnly StartTime { get; set; } = new TimeOnly(0, 0);
        public TimeOnly EndTime { get; set; } = new TimeOnly(23, 59);
        public int SelectLocation { get; set; } = 0;
        public int SelectMessage { get; set; } = 0;
    }

    public class InfoAbonColumn
    {
        public int SurnameColumn { get; set; } = -1;
        public int PhoneColumn { get; set; } = -1;
        public int ArrearsColumn { get; set; } = -1;
        public int AddressColumn { get; set; } = -1;
        public int CodeColumn { get; set; } = -1;
        public int ContractColumn { get; set; } = -1;
    }
}
