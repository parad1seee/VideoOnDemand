using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using VideoOnDemand.Common.Exceptions;
using VideoOnDemand.DAL.Abstract;
using VideoOnDemand.Models.Enums;
using VideoOnDemand.Models.ResponseModels;
using VideoOnDemand.Services.Interfaces;
using VideoOnDemand.Services.Interfaces.External;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Image = VideoOnDemand.Domain.Entities.Image;
using MediaToolkit.Model;

namespace VideoOnDemand.Services.Services
{
    public class ImageService : IImageService
    {
        private IConfiguration _configuration;
        private IUnitOfWork _unitOfWork;
        private IS3Service _s3Service;
        private IMapper _mapper;

        private const string COMPACT_KEY = "_min";
        private const int MIN_IMAGE_HEIGHT = 72;
        private const int MIN_IMAGE_WIDTH = 72;
        private const int IMAGE_COMPACT_WIDTH = 1024;
        private const int IMAGE_COMPACT_HEIGHT = 1024;

        // 10 MB
        private const int MAX_FILE_SIZE = 31457280;
        public string Extensions { get; private set; } = ".png|.jpeg|.jpg";

        // Restriction for server correct work
        private const int MAX_IMAGE_SIDE = 2000;

        public ImageService(IConfiguration configuration, IUnitOfWork unitOfWork,
            IS3Service s3Service, IMapper mapper)
        {
            _configuration = configuration;
            _unitOfWork = unitOfWork;
            _s3Service = s3Service;
            _mapper = mapper;
        }

        public async Task UploadVideo(IFormFile file)
        {
            try
            {
                if (file.Length > MAX_FILE_SIZE)
                    throw new CustomException(HttpStatusCode.BadRequest, "video", "Max file size is 30 MB");

                await Save(file);
            }
            catch (Exception ex)
            {
                throw new CustomException(HttpStatusCode.BadRequest, "file", ex.InnerException?.Message ?? ex.Message);
            }
        }

        public async Task<ImageResponseModel> UploadOne(IFormFile file, ImageType type, bool isS3LinkOpen)
        {
            Validate(file);

            // Define image uploading sizes 
            var uploadingSize = GetSize(type);

            var response = new ImageResponseModel();

            try
            {
                using (var fileStream = file.OpenReadStream())
                {
                    var fileInfo = SixLabors.ImageSharp.Image.Identify(fileStream);

                    // Get image compact size
                    var compactSize = GetCompactImageSize(fileInfo.Width, fileInfo.Height, type, file.FileName);

                    response = await Save(fileStream, file.FileName, uploadingSize, compactSize, isS3LinkOpen);
                }

                return response;
            }
            catch (Exception ex)
            {
                throw new CustomException(HttpStatusCode.BadRequest, "file", ex.InnerException?.Message ?? ex.Message);
            }
        }

        public async Task<List<MultipleImagesResponseModel>> UploadMultipleSavingValid(List<IFormFile> images, ImageType type, bool isS3LinkOpen)
        {
            try
            {
                // Define image uploading sizes 
                var uploadingSize = GetSize(type);

                var result = new List<MultipleImagesResponseModel>();

                // If image doesn`t pass at least one check - change status and pass to next iteration
                foreach (var y in images)
                {
                    // Add name to show wich image incorrect
                    var data = new MultipleImagesResponseModel { Name = y.FileName };
                    result.Add(data);

                    var extention = Path.GetExtension(y.FileName).ToLower();

                    if (y.Length > MAX_FILE_SIZE)
                    {
                        data.Status = ImageSaveStatus.InvalidLength;
                        continue;
                    }

                    if (!Extensions.Contains(extention))
                    {
                        data.Status = ImageSaveStatus.InvalidFormat;
                        continue;
                    }

                    var response = new ImageResponseModel();
                    using (var fileStream = y.OpenReadStream())
                    {
                        var fileInfo = SixLabors.ImageSharp.Image.Identify(fileStream);

                        // Restriction for server correct work
                        if (fileInfo.Width > MAX_IMAGE_SIDE || fileInfo.Height > MAX_IMAGE_SIDE)
                        {
                            data.Status = ImageSaveStatus.InvalidSize;
                            continue;
                        }

                        // Set default image dimention
                        var compactSize = new Size { Height = fileInfo.Height, Width = fileInfo.Width };

                        switch (type)
                        {
                            case ImageType.Square:

                                if (fileInfo.Width < MIN_IMAGE_WIDTH || fileInfo.Height < MIN_IMAGE_HEIGHT)
                                {
                                    data.Status = ImageSaveStatus.InvalidDimension;
                                    continue;
                                }

                                if (Math.Abs(fileInfo.Width - fileInfo.Height) > 0)
                                {
                                    data.Status = ImageSaveStatus.InvalidDimension;
                                    continue;
                                }

                                if (fileInfo.Width > IMAGE_COMPACT_WIDTH || fileInfo.Height > IMAGE_COMPACT_HEIGHT)
                                {
                                    compactSize.Width = IMAGE_COMPACT_WIDTH;
                                    compactSize.Height = IMAGE_COMPACT_HEIGHT;
                                }

                                break;
                            case ImageType.Normal:

                                if (fileInfo.Height < MIN_IMAGE_HEIGHT || fileInfo.Width < MIN_IMAGE_WIDTH)
                                {
                                    data.Status = ImageSaveStatus.InvalidDimension;
                                    continue;
                                }

                                // Passing zero for one of height or width will automatically preserve the aspect ratio of the original image
                                if (fileInfo.Width > IMAGE_COMPACT_WIDTH || fileInfo.Height > IMAGE_COMPACT_HEIGHT)
                                {
                                    if (fileInfo.Width > fileInfo.Height)
                                    {
                                        compactSize.Width = IMAGE_COMPACT_WIDTH;
                                        compactSize.Height = 0;
                                    }
                                    else
                                    {
                                        compactSize.Height = IMAGE_COMPACT_HEIGHT;
                                        compactSize.Width = 0;
                                    }
                                }

                                break;
                        }

                        response = await Save(fileStream, y.FileName, uploadingSize, compactSize, isS3LinkOpen);

                        // In case of success change status and return image response model
                        data.Status = ImageSaveStatus.Saved;
                        data.Image = _mapper.Map<ImageResponseModel>(response);
                    }
                }

                // Throw an exception there are no valid images
                if (result.Any(x => x.Status == ImageSaveStatus.Saved))
                    return result;
                else
                    throw new CustomException(HttpStatusCode.BadRequest, "images", "No image has been saved");
            }
            catch (Exception ex)
            {
                throw new CustomException(HttpStatusCode.BadRequest, "images", ex.InnerException?.Message ?? ex.Message);
            }
        }

        public async Task<List<ImageResponseModel>> UploadMultiple(List<IFormFile> images, ImageType type, bool isS3LinkOpen)
        {
            try
            {
                // Define image uploading sizes 
                var uploadingSize = GetSize(type);

                var result = new List<ImageResponseModel>();

                foreach (var file in images)
                {
                    Validate(file);

                    using (var fileStream = file.OpenReadStream())
                    {
                        var fileInfo = SixLabors.ImageSharp.Image.Identify(fileStream);

                        // Get image compact size
                        var compactSize = GetCompactImageSize(fileInfo.Width, fileInfo.Height, type, file.FileName);

                        var image = await Save(fileStream, file.FileName, uploadingSize, compactSize, isS3LinkOpen);
                        result.Add(image);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new CustomException(HttpStatusCode.BadRequest, "file", ex.InnerException?.Message ?? ex.Message);
            }
        }

        public async Task RemoveImage(int imageId)
        {
            var image = _unitOfWork.Repository<Image>().Find(i => i.Id == imageId);

            if (image == null)
                throw new CustomException(HttpStatusCode.BadRequest, "imageId", "Invalid image id");


            // Remove images from S3
            //await _s3Service.DeleteFile(image.OriginalPath.Substring(image.OriginalPath.LastIndexOf("/") + 1));

            //if (image.CompactPath != null)
            //    await _s3Service.DeleteFile(image.CompactPath.Substring(image.CompactPath.LastIndexOf("/") + 1));

            _unitOfWork.Repository<Image>().Delete(image);
            _unitOfWork.SaveChanges();
        }

        private void Validate(IFormFile file)
        {
            var fileName = file.FileName;
            if (file.Length > MAX_FILE_SIZE)
                throw new CustomException(HttpStatusCode.BadRequest, "Image", $"{fileName} Invalid image size");

            var extention = Path.GetExtension(fileName).ToLower();

            if (!Extensions.Contains(extention))
                throw new CustomException(HttpStatusCode.BadRequest, "Image", $"{fileName} Invalid image format {Extensions}");
        }

        private static Size GetCompactImageSize(int width, int height, ImageType type, string fileName)
        {
            // Restriction for server correct work
            if (width > MAX_IMAGE_SIDE || height > MAX_IMAGE_SIDE)
                throw new CustomException(HttpStatusCode.BadRequest, "Image", $"{fileName} Maximum image side is {MAX_IMAGE_SIDE} px");

            // Set default image dimention
            Size res = new Size { Height = height, Width = width };

            switch (type)
            {
                case ImageType.Square:

                    if (width < MIN_IMAGE_WIDTH || height < MIN_IMAGE_HEIGHT)
                        throw new CustomException(HttpStatusCode.BadRequest, "Image", $"{fileName} Invalid image dimension {MIN_IMAGE_WIDTH}x{MIN_IMAGE_HEIGHT}");

                    if (Math.Abs(width - height) > 0)
                        throw new CustomException(HttpStatusCode.BadRequest, "Image", $"{fileName} Invalid image dimension");

                    if (width > IMAGE_COMPACT_WIDTH || height > IMAGE_COMPACT_HEIGHT)
                    {
                        res.Width = IMAGE_COMPACT_WIDTH;
                        res.Height = IMAGE_COMPACT_HEIGHT;
                    }

                    break;
                case ImageType.Normal:

                    if (height < MIN_IMAGE_HEIGHT || width < MIN_IMAGE_WIDTH)
                        throw new CustomException(HttpStatusCode.BadRequest, "Image", $"{fileName} Invalid image dimension");

                    // Passing zero for one of height or width will automatically preserve the aspect ratio of the original image
                    if (width > IMAGE_COMPACT_WIDTH || height > IMAGE_COMPACT_HEIGHT)
                    {
                        if (width > height)
                        {
                            res.Width = IMAGE_COMPACT_WIDTH;
                            res.Height = 0;
                        }
                        else
                        {
                            res.Height = IMAGE_COMPACT_HEIGHT;
                            res.Width = 0;
                        }
                    }

                    break;
            }

            return res;
        }

        private async Task Save(IFormFile file)
        {
            using (var fileStream = file.OpenReadStream())
            {
                fileStream.Seek(0, SeekOrigin.Begin);

                var key = Guid.NewGuid().ToString();
                var ext = Path.GetExtension(file.FileName);

                await _s3Service.UploadFile(fileStream, key + ext);
            }
        }

        private async Task<ImageResponseModel> Save(Stream fileStream, string fileName, ImageUploadingSize uploadingSize, Size compactSize, bool isS3LinkOpen)
        {
            var response = new Image();
            fileStream.Seek(0, SeekOrigin.Begin);

            using (var image = SixLabors.ImageSharp.Image.Load(fileStream, out IImageFormat format))
            {
                var key = Guid.NewGuid().ToString();
                var ext = Path.GetExtension(fileName);

                if (uploadingSize != ImageUploadingSize.Compact)
                    //response.OriginalPath = "Test";
                    response.OriginalPath = await _s3Service.UploadFile(fileStream, key + ext, isS3LinkOpen);

                if (uploadingSize != ImageUploadingSize.Normal)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        image.Mutate(x => x.Resize(compactSize));
                        image.Save(memoryStream, format);

                        key += COMPACT_KEY;
                        response.CompactPath = "Test_min";
                        //response.CompactPath = await _s3Service.UploadFile(memoryStream, key + ext, isS3LinkOpen);
                    }
                }
            }

            response.IsActive = true;
            _unitOfWork.Repository<Image>().Insert(response);
            _unitOfWork.SaveChanges();

            return _mapper.Map<ImageResponseModel>(response);
        }

        private static ImageUploadingSize GetSize(ImageType type)
        {
            var res = ImageUploadingSize.All;

            switch (type)
            {
                case ImageType.Square:
                    break;
                case ImageType.Normal:
                    break;
                default:
                    break;
            }

            return res;
        }

        public enum ImageUploadingSize
        {
            /// <summary>
            /// All sizes
            /// </summary>
            All,

            /// <summary>
            /// Only normal
            /// </summary>
            Normal,

            /// <summary>
            /// Only compact
            /// </summary>
            Compact,
        }

        public async Task<ImageResponseModel> DownloadImageFromUrl(string url, string fileName, bool isS3LinkOpen)
        {
            if (url == null || url.Length < 1)
                throw new CustomException(HttpStatusCode.BadRequest, "url", "Invalid image url");

            var uploadingSize = ImageUploadingSize.All;
            var response = new ImageResponseModel();

            var uri = new Uri(url);
            var webRequest = (HttpWebRequest)WebRequest.Create(uri);

            try
            {
                var client = new HttpClient();
                var imageResponse = await client.GetAsync(url);
                var arr = await imageResponse.Content.ReadAsByteArrayAsync();

                using (HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse())
                {
                    using (Stream stream = new MemoryStream(arr))
                    {
                        var fileInfo = SixLabors.ImageSharp.Image.Identify(stream);

                        var compactSize = GetCompactImageSize(fileInfo.Width, fileInfo.Height, ImageType.Normal, fileName);

                        response = await Save(stream, fileName, uploadingSize, compactSize, isS3LinkOpen);
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                throw new CustomException(HttpStatusCode.BadRequest, "file", ex.Message);
            }
        }
    }
}
