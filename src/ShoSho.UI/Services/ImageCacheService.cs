using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ShoSho.UI.Services
{
    public static class ImageCacheService
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        public static async Task<string?> GetLocalPosterAsync(string webUrl)
        {
            if (string.IsNullOrWhiteSpace(webUrl)) return null;

            try
            {
                // Шлях до кешу: %LocalAppData%/ShoSho/Cache
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string cacheFolder = Path.Combine(localAppData, "ShoSho", "Cache");

                if (!Directory.Exists(cacheFolder))
                {
                    Directory.CreateDirectory(cacheFolder);
                }

                byte[] hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(webUrl));
                string fileName = Convert.ToHexString(hashBytes) + ".jpg";
                string localFilePath = Path.Combine(cacheFolder, fileName);

                if (File.Exists(localFilePath))
                {
                    return localFilePath;
                }

                byte[] imageBytes = await _httpClient.GetByteArrayAsync(webUrl);
                await File.WriteAllBytesAsync(localFilePath, imageBytes);

                return localFilePath;
            }
            catch
            {
                return null;
            }
        }
    }
}