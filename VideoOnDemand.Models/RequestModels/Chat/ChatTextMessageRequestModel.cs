using VideoOnDemand.Common.Attributes;
using System.ComponentModel.DataAnnotations;

namespace VideoOnDemand.Models.RequestModels.Chat
{
    public class ChatMessageRequestModel
    {
        [TrimmedStringLength(300, ErrorMessage = "{0} must be from {2} to {1} symbols", MinimumLength = 1)]
        public string Text { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "{0} is invalid")]
        public int? ImageId { get; set; }
    }
}
