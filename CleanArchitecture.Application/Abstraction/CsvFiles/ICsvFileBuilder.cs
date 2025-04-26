using CleanArchitecture.Application.Users.Queries.GetUser;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Abstraction.CsvFiles
{
    public interface ICsvFileBuilder
    {
        byte[] BuildUsersFile(IEnumerable<GetUserResponse> Users);
    }
}
