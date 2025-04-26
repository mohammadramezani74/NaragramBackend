using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Common.Models
{
  
        public class IntsAuthResult
        {
            public int count { get; set; }
            public Result result { get; set; }
            public int status { get; set; }
            public string message { get; set; }
            public bool isSucceded { get; set; }
        }

        public class Result
        {
            public Guid id { get; set; }
            public string firstName { get; set; }
            public string lastName { get; set; }
            public string city { get; set; }
            public string chartPost { get; set; }
        }

    
}
