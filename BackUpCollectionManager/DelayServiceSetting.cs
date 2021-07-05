using System;
using System.Collections.Generic;
using System.Text;

namespace BackUpCollectionManager
{
    public class DelayServiceSetting
    {
        public string ServiceName { get; set; }
        public Dictionary<string, string> ConnectionStrings { get; set; }
        public int DelayMs { get; set; }
        public int PolicyDays { get; set; }

    }
}
