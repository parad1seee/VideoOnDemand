using System.ComponentModel.DataAnnotations;

namespace VideoOnDemand.Models.ResponseModels
{
    public class ImageResponseModel
    {
        [Required]
        public int Id { get; set; }

        public string OriginalPath { get; set; }

        public string CompactPath { get; set; }
    }
}
