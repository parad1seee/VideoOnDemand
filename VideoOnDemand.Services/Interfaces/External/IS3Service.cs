using System.IO;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Interfaces.External
{
    public interface IS3Service
    {
        /// <summary>
        /// Uploads file to AWS S3 source
        /// </summary>
        /// <param name="stream">uploading file stream</param>
        /// <param name="key">output file name</param>
        /// <param name="isS3LinkOpen">is open file by link from s3</param>
        /// <returns></returns>
        Task<string> UploadFile(Stream stream, string key, bool isS3LinkOpen = false);

        /// <summary>
        /// Remove file from AWS S3
        /// </summary>
        /// <param name="name">File name</param>
        Task DeleteFile(string name);

        /// <summary>
        /// Get file from S3 by name
        /// </summary>
        /// <param name="fileName">file name</param>
        /// <returns></returns>
        Task<byte[]> GetFile(string fileName);
    }
}
