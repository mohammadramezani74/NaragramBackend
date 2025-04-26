using CleanArchitecture.Domain.Entities.Identity;
using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Domain.Common
{
    public class BaseEntity:Entity
    {
        public BaseEntity()
        {
            this.Id = Guid.NewGuid();
            this.Deleted = false;
            CreateDate=DateTime.Now;
        }

        public Guid Id { get; set; }


        public bool Deleted { get; set; }

        public DateTime CreateDate { get; set; }


        public Guid? CreatedByUserId { get; set; }


        public User? CreatedByUser { get; set; }

      
        public DateTime? ModifiedDate { get; set; }

    
        public Guid? ModifiedById { get; set; }

       
        public User? ModifiedBy { get; set; }


    }
}
