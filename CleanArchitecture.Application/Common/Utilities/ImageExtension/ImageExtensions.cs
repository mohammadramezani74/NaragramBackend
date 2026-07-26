using Microsoft.AspNetCore.Http;
using  SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;

namespace CleanArchitecture.Application.Common.Utilities.ImageExtension
{
    public static class ImageExtensions
    {
        public static async Task<byte[]> ConvertFormFileToByte(IFormFile file)
        {
            try
            {
                if (file is null || file.Length == 0) return null;

                // یک تخصیص، دقیقاً به اندازه‌ی فایل.
                var buffer = new byte[file.Length];
                await using var stream = file.OpenReadStream();
                await stream.ReadExactlyAsync(buffer);
                return buffer;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public static async Task<Image> ConvertFormFileToImage(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return null;

                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                Console.WriteLine(file.ContentType);
                return await Image.LoadAsync(memoryStream);

            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public static async Task<Image> GetReducedImage(this IFormFile resourceImage, int width, int height)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                await resourceImage.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                var image = Image.Load(memoryStream);


                image.Mutate(x => x.Resize(width, height));


                return image;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public static async Task<Image<Rgba32>> GenerateThumbnailForVideoAsync(this IFormFile videoFile)
        {
            try
            {
            
                if (videoFile == null || videoFile.Length == 0)
                    throw new ArgumentException("No video file provided");

          
                string tempVideoPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".mp4");

                using (var fileStream = new FileStream(tempVideoPath, FileMode.Create))
                {
                    await videoFile.CopyToAsync(fileStream);
                }


                string tempThumbnailPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");

                string ffmpegArgs = $"-i \"{tempVideoPath}\" -ss 00:00:05 -vframes 1 \"{tempThumbnailPath}\"";

          
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg", 
                    Arguments = ffmpegArgs,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processStartInfo))
                {
                    if (process != null)
                    {
                        await process.WaitForExitAsync();
                    }
                }

             
                var image = Image.Load<Rgba32>(tempThumbnailPath);

             
                File.Delete(tempVideoPath);
                File.Delete(tempThumbnailPath);

              
                return image;
            }
            catch (Exception ex)
            {
                throw new Exception("Error generating thumbnail", ex);
            }
        }

        public static async Task<double> GetMediaDurationAsync(IFormFile mediaFile)
        {
            try
            {
                if (mediaFile == null || mediaFile.Length == 0)
                    throw new ArgumentException("No media file provided");

       
                string tempMediaPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + Path.GetExtension(mediaFile.FileName));


                using (var fileStream = new FileStream(tempMediaPath, FileMode.Create))
                {
                    await mediaFile.CopyToAsync(fileStream);
                }


                string ffmpegArgs = $"-i \"{tempMediaPath}\""; 

            
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = ffmpegArgs,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processStartInfo))
                {
                    if (process != null)
                    {
                        string output = await process.StandardError.ReadToEndAsync(); 
                        process.WaitForExit();

                     
                        string durationString = GetDurationFromFFmpegOutput(output);

                        if (!string.IsNullOrEmpty(durationString))
                        {
                          
                            var timeSpan = TimeSpan.Parse(durationString);
                            return timeSpan.TotalSeconds; 
                        }
                        else
                        {
                            throw new Exception("Duration not found in media file");
                        }
                    }
                }

                return 0; 
            }
            catch (Exception ex)
            {
                throw new Exception("Error extracting media duration", ex);
            }
        }

        private static string GetDurationFromFFmpegOutput(string output)
        {

            const string durationPrefix = "Duration: ";
            int durationStart = output.IndexOf(durationPrefix);

            if (durationStart != -1)
            {
                durationStart += durationPrefix.Length;
                int durationEnd = output.IndexOf(",", durationStart);
                if (durationEnd != -1)
                {
                    return output.Substring(durationStart, durationEnd - durationStart);
                }
            }
            return string.Empty;
        }

    }
}
