using CleanArchitecture.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Domain.ValueObjects.Chat
{
    public  record ContentType
    {
        public string Value { get; init; }

        public static ContentType ImageJpeg = new ContentType("image/jpeg");
        public static ContentType ImagePng = new ContentType("image/png");
        public static ContentType ApplicationPdf = new ContentType("application/pdf");


     private ContentType(string value)=>Value = value;


        public static ContentType Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Content type cannot be empty.");
            }

           
            if (!IsValidMimeType(value))
            {
                throw new ArgumentException($"Invalid content type: {value}");
            }

            return new ContentType(value);
        }

   
        private static bool IsValidMimeType(string value)
        {
            var parts = value.Split('/');
            return parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]) && !string.IsNullOrWhiteSpace(parts[1]);
        }




        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => Value;
    }
}
