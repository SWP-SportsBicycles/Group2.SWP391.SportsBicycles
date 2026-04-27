using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.DTOs
{
    public class AdminDashboardDTO
    {
        public int TotalListings { get; set; }
        public int ActiveListings { get; set; }

        public int TotalUsers { get; set; }
        public int TotalSellers { get; set; }

        public decimal MonthlyRevenue { get; set; }
        public double RevenueGrowthPercent { get; set; }
        public decimal BuyerFeeRevenue { get; set; }
        public decimal SellerFeeRevenue { get; set; }
        public decimal InspectionRevenue { get; set; }
        public int TotalOrders { get; set; }          
        public int CompletedOrders { get; set; }     
        public int ProcessingOrders { get; set; }
        public List<CityStatsDTO> Cities { get; set; } = new();
    }

    public class CityStatsDTO
    {
        public string City { get; set; } = default!;
        public int Listings { get; set; }
        public int Orders { get; set; }
    }
}
