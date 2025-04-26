using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Common.Models
{
    public  class ApplicationSettings
    {
        public JwtInformation Jwt { get; set; } = null!;
        public DatabaseConnection ConnectionStrings { get; set; } = null!;
    }


    public class DatabaseConnection
    {
        public string ApplicationDbContext { get; set; } = null!;
    }
    public class JwtInformation
    {
        public string Secret { get; set; } = null!;
        public string EncryptKey { get; set; } = null!;
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public int ExpirationInMinutes { get; set; } 
        public int ExpirationRefreshInMinutes { get; set; }
    }
}
