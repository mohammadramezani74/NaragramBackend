using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Common.Models
{
    public class BaseGenericViewModel<TModel> : OperationResult
    {

        public TModel result { get; set; }
        public int count { get; set; } = 1;
        public BaseGenericViewModel(TModel model, OperationResult op, int count = 1) : base(op)
        {
            result = model;
            this.count = count;
        }

    }
}
