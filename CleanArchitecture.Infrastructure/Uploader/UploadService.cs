using CleanArchitecture.Application.Abstraction.Uploader;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Uploader
{

    public  class UploadService: IUploaderService
    {
        public  string Upload( IFormFile file,string Name, string path)
        {
            if (file == null) return "";

         
          var  directoryPath= Path.Combine(Directory.GetCurrentDirectory(),
                    "/ChatFiles/",
                   Name,
                  path);
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            var fileName = $"{file.FileName}";
            var filePath = $"{directoryPath}//{fileName}";
            using var output = File.Create(filePath);
            file.CopyTo(output);
            return $"{filePath}";

        }
        public  void DeleteFile(string filePath)
        {
            var fullFilePath = Path.Combine( "ChatFiles", filePath);

            if (File.Exists(fullFilePath))
            {
                File.Delete(fullFilePath);
            }
        }

    }
}
