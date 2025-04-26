using CleanArchitecture.Application.Users.Queries.GetUser;
using CsvHelper.Configuration;
using System.Globalization;


namespace CleanArchitecture.Infrastructure.Files.Map
{
    internal sealed class UserMap: ClassMap<GetUserResponse>
    {
        public UserMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
            Map(m => m.FirstName).Name("نام");
            Map(m => m.LastName).Name("نام خانوادگی");
            Map(m => m.Address.City).Name("شهر");
            Map(m => m.Age).Name("سن");
            Map(m => m.UserName).Name("نام کاربری");

        }
    }
}
