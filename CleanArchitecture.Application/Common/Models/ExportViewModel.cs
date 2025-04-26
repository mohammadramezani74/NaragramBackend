using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Common.Models
{
    public sealed record ExportViewModel
    {
        public ExportViewModel(byte[] content,string Name)
        {
            ContentType = "text/csv";
            Content= content;
            FileName = string.Concat(Name + ".csv");

        }
        public  string FileName { get; set; }=null!;

        public string ContentType { get; set; } = null!;

        public  byte[] Content { get; set; }=null!;
    }
}
