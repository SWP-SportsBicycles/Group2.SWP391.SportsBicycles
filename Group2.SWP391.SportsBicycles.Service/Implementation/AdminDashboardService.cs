using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.DAL.Contract;
using Group2.SWP391.SportsBicycles.DAL.Models;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Implementation
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly IGenericRepository<Listing> _listingRepo;
        private readonly IGenericRepository<User> _userRepo;
        private readonly IGenericRepository<Order> _orderRepo;
        private readonly IGenericRepository<Transaction> _transactionRepo;

        public AdminDashboardService(
            IGenericRepository<Listing> listingRepo,
            IGenericRepository<User> userRepo,
            IGenericRepository<Order> orderRepo,
            IGenericRepository<Transaction> transactionRepo)
        {
            _listingRepo = listingRepo;
            _userRepo = userRepo;
            _orderRepo = orderRepo;
            _transactionRepo = transactionRepo;
        }
        private static ResponseDTO Success(object? data = null)
          => new()
          {
              IsSucess = true,
              BusinessCode = BusinessCode.GET_DATA_SUCCESSFULLY,
              Data = data
          };

        private static ResponseDTO Fail(string msg)
            => new()
            {
                IsSucess = false,
                BusinessCode = BusinessCode.EXCEPTION,
                Message = msg
            };

        public async Task<ResponseDTO> GetDashboardAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var lastMonth = now.AddMonths(-1);

                // ===== LISTING =====
                var listingQuery = _listingRepo.AsQueryable()
                    .Where(x => !x.IsDeleted);

                var totalListings = await listingQuery.CountAsync();

                var activeListings = await listingQuery
                    .CountAsync(x => x.Status == ListingStatusEnum.Published);

                // ===== USER =====
                var userQuery = _userRepo.AsQueryable();

                var totalUsers = await userQuery.CountAsync();

                var totalSellers = await userQuery
                    .CountAsync(x => x.Role == RoleEnum.SELLER); // sửa nếu enum

                // ===== REVENUE (🔥 chuẩn Transaction) =====
                var currentMonthRevenue = await _transactionRepo.AsQueryable()
                    .Where(x =>
                        x.Status == TransactionStatusEnum.Paid &&
                        x.PaidAt.HasValue &&
                        x.PaidAt.Value.Month == now.Month &&
                        x.PaidAt.Value.Year == now.Year)
                    .SumAsync(x => (decimal?)x.Amount) ?? 0;

                var lastMonthRevenue = await _transactionRepo.AsQueryable()
                    .Where(x =>
                        x.Status == TransactionStatusEnum.Paid &&
                        x.PaidAt.HasValue &&
                        x.PaidAt.Value.Month == lastMonth.Month &&
                        x.PaidAt.Value.Year == lastMonth.Year)
                    .SumAsync(x => (decimal?)x.Amount) ?? 0;

                double growthPercent = 0;

                if (lastMonthRevenue > 0)
                {
                    growthPercent = (double)((currentMonthRevenue - lastMonthRevenue)
                        / lastMonthRevenue * 100);
                }

                // ===== CITY STATS =====
                var cities = new[] { "Hà Nội", "TP.HCM", "Đà Nẵng" };

                var cityStats = new List<CityStatsDTO>();

                foreach (var city in cities)
                {
                    var listingCount = await listingQuery
                        .CountAsync(x => x.City == city);

                    var orderCount = await _orderRepo.AsQueryable()
                        .Include(o => o.OrderItems)
                            .ThenInclude(oi => oi.Bike)
                        .CountAsync(o =>
                            o.OrderItems.Any(oi =>
                                oi.Bike != null &&
                                oi.Bike.City == city));

                    cityStats.Add(new CityStatsDTO
                    {
                        City = city,
                        Listings = listingCount,
                        Orders = orderCount
                    });
                }

                var result = new AdminDashboardDTO
                {
                    TotalListings = totalListings,
                    ActiveListings = activeListings,
                    TotalUsers = totalUsers,
                    TotalSellers = totalSellers,
                    MonthlyRevenue = currentMonthRevenue,
                    RevenueGrowthPercent = Math.Round(growthPercent, 2),
                    Cities = cityStats
                };

                return Success(result);
            }
            catch (Exception ex)
            {
                return Fail("Lỗi dashboard: " + ex.Message);
            }
        }
    }
}
