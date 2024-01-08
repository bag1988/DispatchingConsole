using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Models
{
    public class ManualP16Info
    {
        public uint Id { get; set; } = 1;
        public uint CMD_ID { get; set; }
        public uint SerNo { get; set; }
        public uint Cmd { get; set; }
        public string CommandName { get; set; } = string.Empty;
        public string SourceName { get; set; } = string.Empty;

        public DateTime GotIn { get; set; } = DateTime.UtcNow;

        public TimeSpan TimeWait => (DateTime.UtcNow - GotIn);

        public List<string> SitNameList { get; set; } = new();
    }
}
