using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Models
{
    public class BackupInfo
    {
        public string? Name { get; set; }
        public DateTime? Created { get; set; } 
        public long SizeFile { get; set; }

        public string? Url { get; set; }
    }
}
