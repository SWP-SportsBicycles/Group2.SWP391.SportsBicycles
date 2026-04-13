using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.DAL.Models
{
    public class Cart : BaseEntity
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = default!;

        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}
