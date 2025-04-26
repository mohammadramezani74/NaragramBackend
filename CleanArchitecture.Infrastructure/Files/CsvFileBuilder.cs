using CleanArchitecture.Application.Abstraction.CsvFiles;
using CleanArchitecture.Application.Users.Queries.GetUser;
using CleanArchitecture.Infrastructure.Files.Map;
using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Files
{
    internal sealed class CsvFileBuilder: ICsvFileBuilder
    {
        public byte[] BuildUsersFile(IEnumerable<GetUserResponse> Users)
        {
            using var memoryStream = new MemoryStream();
            using (var streamWriter = new StreamWriter(memoryStream, new UTF8Encoding(false))) // بدون BOM
            {
                using var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);

                csvWriter.Context.RegisterClassMap<UserMap>();
                csvWriter.WriteRecords(Users);
            }

            return memoryStream.ToArray();
        }

    }
}
