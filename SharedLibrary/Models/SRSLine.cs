using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Models
{
    public class SRSLine
    {
        public int Id { get; set; }
        public uint Port { get; set; }
        public uint Version { get; set; }
        public uint Line { get; set; }
        public uint StaffID { get; set; }
        public uint SubSystID { get; set; }
        public uint SitID { get; set; }
    }
}
