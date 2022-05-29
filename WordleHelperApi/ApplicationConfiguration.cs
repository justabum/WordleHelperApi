using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WordleHelperApi
{
    public class ApplicationConfiguration
    {
        public int MinimumLevel { get; set; }
        public string AboutMessage { get; set; }
        public string UserID { get; set; }
        public string Password { get; set; }
        public string Server { get; set; }
    }
}
