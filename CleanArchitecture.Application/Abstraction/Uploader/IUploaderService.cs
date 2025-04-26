using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace CleanArchitecture.Application.Abstraction.Uploader;

public interface IUploaderService
{
    string Upload(IFormFile file, string UserName, string path);
    void DeleteFile(string filePath);
}
