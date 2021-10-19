using VideoOnDemand.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace VideoOnDemand.Domain.Entities
{
    public class Image : IEntity
    {
        #region Properties

        [Key]
        public int Id { get; set; }

        public string OriginalPath { get; set; }

        public string CompactPath { get; set; }

        public bool IsActive { get; set; }

        public bool IsUsed { get; set; }

        #endregion
    }
}
