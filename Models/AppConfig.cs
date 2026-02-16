using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CbetaTranslator.App.Models
{
    public sealed class AppConfig
    {
        public string? TextRootPath { get; set; }
        public string? LastSelectedRelPath { get; set; } // optional for later
        public int Version { get; set; } = 1;
        public bool ZenOnly { get; set; }
    }

}
