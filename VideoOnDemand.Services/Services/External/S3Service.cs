using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VideoOnDemand.Common.Exceptions;
using VideoOnDemand.Services.Interfaces.External;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Services.External
{
    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IConfiguration _configuration;
        private ILogger<S3Service> _logger;

        private readonly string _urlTemplate;
        private readonly string _bucket;
        private readonly string _folder;
        private readonly string _serverUrlTemplate;

        public S3Service(IConfiguration configuration, IAmazonS3 s3Client, ILogger<S3Service> logger)
        {
            _configuration = configuration;
            _s3Client = s3Client;
            _logger = logger;

            _urlTemplate = _configuration["AWS:UrlTemplate"];
            _bucket = _configuration["AWS:Bucket"];
            _folder = _configuration["AWS:Folder"];

            // server name + /api/v1/content/
            _serverUrlTemplate = _configuration["AWS:Image:UrlTemplate"];
        }

        public async Task<string> UploadFile(Stream stream, string key, bool isS3LinkOpen = false)
        {
            // allow open file by S3 link
            var additionalProperties = new Dictionary<string, object>()
            {
                { "CannedACL",  S3CannedACL.PublicRead }
            };

            if (isS3LinkOpen)
            {
                await _s3Client.UploadObjectFromStreamAsync(_bucket, $"{_folder}/{key}", stream, additionalProperties);
                return string.Format(_urlTemplate, _bucket, _folder, key);
            }
            else
            {
                await _s3Client.UploadObjectFromStreamAsync(_bucket, $"{_folder}/{key}", stream, null);
                return string.Format(_serverUrlTemplate, key);
            }
        }

        public async Task DeleteFile(string name)
        {
            var response = await _s3Client.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = _bucket,
                Key = name
            });
        }

        public async Task<byte[]> GetFile(string fileName)
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucket,
                Key = $"{fileName}"
            };

            try
            {
                using (GetObjectResponse response = await _s3Client.GetObjectAsync(request))
                {
                    using (Stream responseStream = response.ResponseStream)
                    {
                        var arr = new byte[] { };

                        using (var stream = new MemoryStream())
                        {
                            await responseStream.CopyToAsync(stream);
                            arr = stream.ToArray();
                        }

                        return arr;
                    }
                }
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, $"Amazon S3 Exception -> {ex.Message}", ex);
                throw new CustomException(HttpStatusCode.BadRequest, "file", "Error while getting file");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message, ex);
                throw new CustomException(HttpStatusCode.BadRequest, "file", "Error while getting file");
            }
        }

        private async Task EnsureBucketCreatedAsync(string bucketName)
        {
            if (!(await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName)))
            {
                throw new AmazonS3Exception(string.Format("Bucket is missing", bucketName));
            }
        }
    }
}
