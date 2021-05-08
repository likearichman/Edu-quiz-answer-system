using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaselineSystem
{
    public class Element
    {
        public class Passages
        {
            public bool is_selected { get; set; }
            public string url { get; set; }
            public string passage_text { get; set; }
            public int passage_id { get; set; }
            public string title
            {
                get
                {
                    if (url.LastIndexOf(@"/") == url.Length-1)
                        url = url.Substring(0, url.Length-1);
                    return url.Substring(url.LastIndexOf(@"/") + 1);
                }
            }
        }
        public Passages[] passages { get; set; }
        public int query_id { get; set; }
        //public string[] answers { get; set; }
        public string query_type { get; set; }
        public string query { get; set; }

    }
}
