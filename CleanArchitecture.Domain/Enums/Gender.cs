using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Domain.Enums
{
    public enum Gender
    {
        [Display(Name = "مرد")]
        Unknown = 0,
        [Display(Name ="مرد")]
        Male=1,
        [Display(Name = "زن")]
        Female =2
    }
}
