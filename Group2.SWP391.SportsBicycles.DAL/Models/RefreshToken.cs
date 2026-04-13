using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.DAL.Models
{
    public class RefreshToken : BaseEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [MaxLength(512)]
        public string Token { get; set; } = string.Empty;

        public DateTime ExpiredAt { get; set; }

        public bool Revoked { get; set; } = false;

        [MaxLength(255)]
        public string? DeviceInfo { get; set; }

        [MaxLength(255)]
        public string? IpAddress { get; set; }

        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;
    }
}
