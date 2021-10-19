using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoOnDemand.Services.Interfaces;
using VideoOnDemand.Services.Interfaces.External;

namespace VideoOnDemand.Services.Services
{
    public class VideoService : IVideoService
    {
        private readonly IS3Service _s3Service;

        public async Task Upload(IFormFile video)
        {
            
        }
    }
}
