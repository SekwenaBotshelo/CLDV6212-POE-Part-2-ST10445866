using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.Text;

namespace ABCRetailers.Functions.Helpers;

public static class MultipartHelper
{
    public static async Task<Dictionary<string, byte[]>> ParseMultipartAsync(Stream body, string contentType)
    {
        var result = new Dictionary<string, byte[]>();
        var boundary = HeaderUtilities.RemoveQuotes(MediaTypeHeaderValue.Parse(contentType).Boundary).Value;
        var reader = new MultipartReader(boundary, body);

        MultipartSection section;
        while ((section = await reader.ReadNextSectionAsync()) != null)
        {
            var contentDisposition = ContentDispositionHeaderValue.Parse(section.ContentDisposition);
            var fileName = contentDisposition.FileName.Value ?? contentDisposition.FileNameStar.Value;

            if (!string.IsNullOrEmpty(fileName))
            {
                using var ms = new MemoryStream();
                await section.Body.CopyToAsync(ms);
                result[fileName] = ms.ToArray();
            }
        }

        return result;
    }
}