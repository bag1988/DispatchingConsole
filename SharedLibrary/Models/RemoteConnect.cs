using StaffDataProto.V1;
using SharedLibrary.GlobalEnums;

namespace SharedLibrary.Models
{
    public class RemoteConnect
    {
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string? IpAdress { get; set; }
        public int? CuType { get; set; }
        public int? ReceiverStaffID { get; set; }
        public RemoteCmdType TypeProssec { get; set; }
        public bool IsHoliday { get; set; }
        public StaffConnParams? ConnParams { get; set; }
    }
}
