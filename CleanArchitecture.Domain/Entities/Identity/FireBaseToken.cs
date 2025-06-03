using CleanArchitecture.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Domain.Entities.Identity
{
    public class FireBaseToken: BaseEntity
    {
        public  string  Token { get;private set; }
        public Guid UserId { get; private set; }
        public User User { get; private set; }


        public static FireBaseToken CreateUserToken(string token, Guid UserId)
            => new FireBaseToken
            {
                Id = Guid.NewGuid(),
                Token = token,
                Deleted = false,
                CreateDate = DateTime.Now,
                UserId = UserId

            };
        public void update(string token)
        {
            Token = token;
            ModifiedDate = DateTime.Now;
        }
    }
}
