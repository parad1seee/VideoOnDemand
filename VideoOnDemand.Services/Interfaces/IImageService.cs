using Microsoft.AspNetCore.Http;
using VideoOnDemand.Models.Enums;
using VideoOnDemand.Models.ResponseModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Interfaces
{
    public interface IImageService
    {
        /// <summary>
        /// Supported file extensions
        /// </summary>
        string Extensions { get; }

        Task UploadVideo(IFormFile file);

        /// <summary>
        /// Validate and save image file
        /// </summary>
        /// <param name="image">image file</param>
        /// <param name="type">Squere or normal</param>
        /// <param name="isS3LinkOpen">is open file by link from s3</param>
        /// <returns>Model with image paths</returns>
        Task<ImageResponseModel> UploadOne(IFormFile image, ImageType type, bool isS3LinkOpen);

        /// <summary>
        /// Validate and save multiple images
        /// </summary>
        /// <param name="images">Images</param>
        /// <param name="type">Images type</param>
        /// <param name="isS3LinkOpen">is open file by link from s3</param>
        /// <returns>Model with images and statuses</returns>
        Task<List<MultipleImagesResponseModel>> UploadMultipleSavingValid(List<IFormFile> images, ImageType type, bool isS3LinkOpen);

        /// <summary>
        /// Save multiple images only if all images are valid 
        /// </summary>
        /// <param name="images">Images</param>
        /// <param name="type">Images type</param>
        /// <param name="isS3LinkOpen">Images type</param>
        /// <returns>Models with image paths</returns>
        Task<List<ImageResponseModel>> UploadMultiple(List<IFormFile> images, ImageType type, bool isS3LinkOpen);

        /// <summary>
        /// Remove image
        /// </summary>
        /// <param name="imageId"></param>
        /// <returns></returns>
        Task RemoveImage(int imageId);

        /// <summary>
        /// Download image from url and save to S3
        /// </summary>
        /// <param name="url"></param>
        /// <param name="fileName"></param>
        /// <param name="isS3LinkOpen">is open file by link from s3</param>
        /// <returns></returns>
        Task<ImageResponseModel> DownloadImageFromUrl(string url, string fileName, bool isS3LinkOpen);
    }
}
