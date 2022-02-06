using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLearn.Core.Plugins.Model
{
    public class SharedAccessConnection
    {
        public string Endpoint { get; set; }
        public string SharedAccessKeyName { get; set; }
        public string SharedAccessKey { get; set; }
        public string EntityPath { get; set; }
    }
}
