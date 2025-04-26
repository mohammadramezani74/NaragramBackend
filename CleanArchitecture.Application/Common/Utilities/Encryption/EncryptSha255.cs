using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace CleanArchitecture.Application.Common.Utilities.Encryption
{
    public static class EncryptSha255
    {
        public static string GetSha256Hash(this string input)
        {
            using (var hashAlgorithm = SHA256.Create()) 
            {
                var byteValue = Encoding.UTF8.GetBytes(input);
                var byteHash = hashAlgorithm.ComputeHash(byteValue);
                return Convert.ToBase64String(byteHash);
            }
        }
    }
}
