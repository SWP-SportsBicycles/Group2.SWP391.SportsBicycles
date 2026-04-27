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

        //public async Task<ResponseDTO> GetDashboardAsync()
        //{
        //    try
        //    {
        //        var now = DateTime.UtcNow;
        //        var lastMonth = now.AddMonths(-1);

        //        // ===== LISTING =====
        //        var listingQuery = _listingRepo.AsQueryable()
        //            .Where(x => !x.IsDeleted);

        //        var totalListings = await listingQuery.CountAsync();

        //        var activeListings = await listingQuery
        //            .CountAsync(x => x.Status == ListingStatusEnum.Published);

        //        // ===== USER =====
        //        var userQuery = _userRepo.AsQueryable();

        //        var totalUsers = await userQuery.CountAsync();

        //        var totalSellers = await userQuery
        //            .CountAsync(x => x.Role == RoleEnum.SELLER);

        //        // ================= REVENUE (🔥 FIX CHUẨN) =================

        //        var currentMonthOrders = await _orderRepo.AsQueryable()
        //            .Include(o => o.OrderItems)
        //                .ThenInclude(oi => oi.Bike)
        //            .Where(o =>
        //                o.Status == OrderStatusEnum.Completed &&
        //                o.CompletedAt.HasValue &&
        //                o.CompletedAt.Value.Month == now.Month &&
        //                o.CompletedAt.Value.Year == now.Year)
        //            .ToListAsync();

        //        decimal buyerFeeRevenue = 0;
        //        decimal sellerFeeRevenue = 0;
        //        decimal inspectionRevenue = 0;

        //        foreach (var order in currentMonthOrders)
        //        {
        //            var bike = order.OrderItems.First().Bike;
        //            var originalPrice = bike.OriginalPrice;

        //            buyerFeeRevenue += originalPrice * 0.05m;
        //            sellerFeeRevenue += originalPrice * 0.05m;
        //            inspectionRevenue += 100000;
        //        }

        //        decimal totalRevenue = buyerFeeRevenue + sellerFeeRevenue + inspectionRevenue;

        //        // ===== LAST MONTH =====
        //        var lastMonthOrders = await _orderRepo.AsQueryable()
        //            .Include(o => o.OrderItems)
        //                .ThenInclude(oi => oi.Bike)
        //            .Where(o =>
        //                o.Status == OrderStatusEnum.Completed &&
        //                o.CompletedAt.HasValue &&
        //                o.CompletedAt.Value.Month == lastMonth.Month &&
        //                o.CompletedAt.Value.Year == lastMonth.Year)
        //            .ToListAsync();

        //        decimal lastMonthRevenue = 0;

        //        foreach (var order in lastMonthOrders)
        //        {
        //            var bike = order.OrderItems.First().Bike;
        //            var originalPrice = bike.OriginalPrice;

        //            lastMonthRevenue += (originalPrice * 0.05m) // buyer
        //                              + (originalPrice * 0.05m) // seller
        //                              + 100000; // inspection
        //        }

        //        double growthPercent = 0;

        //        if (lastMonthRevenue > 0)
        //        {
        //            growthPercent = (double)((totalRevenue - lastMonthRevenue)
        //                / lastMonthRevenue * 100);
        //        }

        //        // ===== CITY =====
        //        var cities = new[] { "Hà Nội", "TP.HCM", "Đà Nẵng" };

        //        var cityStats = new List<CityStatsDTO>();

        //        foreach (var city in cities)
        //        {
        //            var listingCount = await listingQuery
        //                .CountAsync(x => x.City == city);

        //            var orderCount = await _orderRepo.AsQueryable()
        //                .Include(o => o.OrderItems)
        //                    .ThenInclude(oi => oi.Bike)
        //                .CountAsync(o =>
        //                    o.OrderItems.Any(oi =>
        //                        oi.Bike != null &&
        //                        oi.Bike.City == city));

        //            cityStats.Add(new CityStatsDTO
        //            {
        //                City = city,
        //                Listings = listingCount,
        //                Orders = orderCount
        //            });
        //        }

        //        // ===== RESULT =====
        //        var result = new AdminDashboardDTO
        //        {
        //            TotalListings = totalListings,
        //            ActiveListings = activeListings,
        //            TotalUsers = totalUsers,
        //            TotalSellers = totalSellers,


        //            BuyerFeeRevenue = buyerFeeRevenue,
        //            SellerFeeRevenue = sellerFeeRevenue,
        //            InspectionRevenue = inspectionRevenue,
        //            MonthlyRevenue = totalRevenue,

        //            RevenueGrowthPercent = Math.Round(growthPercent, 2),
        //            Cities = cityStats
        //        };

        //        return Success(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Fail("Lỗi dashboard: " + ex.Message);
        //    }
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
                    .CountAsync(x => x.Role == RoleEnum.SELLER);
                var totalOrders = await _orderRepo.AsQueryable()
                    .Include(o => o.Transaction)
                    .CountAsync(o =>
                        !o.IsDeleted &&
                        o.Transaction != null &&
                        o.Transaction.Status == TransactionStatusEnum.Paid);
                var completedOrders = await _orderRepo.AsQueryable()
                    .Include(o => o.Transaction)
                    .CountAsync(o =>
                        o.Status == OrderStatusEnum.Completed &&
                        o.Transaction != null &&
                        o.Transaction.Status == TransactionStatusEnum.Paid);
                var processingOrders = await _orderRepo.AsQueryable()
                    .Include(o => o.Transaction)
                    .CountAsync(o =>
                        o.Status != OrderStatusEnum.Completed &&
                        o.Transaction != null &&
                        o.Transaction.Status == TransactionStatusEnum.Paid);

                // ================= REVENUE (🔥 FIX CHUẨN) =================

                var currentMonthOrders = await _orderRepo.AsQueryable()
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Bike)
                    .Include(o => o.Transaction)
                    .Where(o =>
                        o.Status == OrderStatusEnum.Completed &&
                        o.Transaction != null &&
                        o.Transaction.Status == TransactionStatusEnum.Paid &&
                        o.Transaction.PaidAt.HasValue &&
                        o.Transaction.PaidAt.Value.Month == now.Month &&
                        o.Transaction.PaidAt.Value.Year == now.Year)
                    .ToListAsync();

                decimal buyerFeeRevenue = 0;
                decimal sellerFeeRevenue = 0;
                decimal inspectionRevenue = 0;

                foreach (var order in currentMonthOrders)
                {
                    var bike = order.OrderItems.First().Bike;

                    decimal originalPrice = bike.OriginalPrice;
                    decimal paidAmount = order.Transaction.Amount;

                    // 🔥 BUYER FEE (CHUẨN)
                    buyerFeeRevenue += (paidAmount - originalPrice);

                    // 🔥 SELLER FEE
                    sellerFeeRevenue += originalPrice * 0.05m;

                    // 🔥 INSPECTION
                    inspectionRevenue += 100000;
                }

                decimal totalRevenue = buyerFeeRevenue + sellerFeeRevenue + inspectionRevenue;

                // ================= LAST MONTH =================

                var lastMonthOrders = await _orderRepo.AsQueryable()
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Bike)
                    .Include(o => o.Transaction)
                    .Where(o =>
                        o.Status == OrderStatusEnum.Completed &&
                        o.Transaction != null &&
                        o.Transaction.Status == TransactionStatusEnum.Paid &&
                        o.Transaction.PaidAt.HasValue &&
                        o.Transaction.PaidAt.Value.Month == lastMonth.Month &&
                        o.Transaction.PaidAt.Value.Year == lastMonth.Year)
                    .ToListAsync();

                decimal lastMonthRevenue = 0;

                foreach (var order in lastMonthOrders)
                {
                    var bike = order.OrderItems.First().Bike;

                    decimal originalPrice = bike.OriginalPrice;
                    decimal paidAmount = order.Transaction.Amount;

                    decimal buyerFee = (paidAmount - originalPrice);
                    decimal sellerFee = originalPrice * 0.05m;
                    decimal inspection = 100000;

                    lastMonthRevenue += buyerFee + sellerFee + inspection;
                }

                double growthPercent = 0;

                if (lastMonthRevenue > 0)
                {
                    growthPercent = (double)((totalRevenue - lastMonthRevenue)
                        / lastMonthRevenue * 100);
                }

                // ===== CITY =====
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

                // ===== RESULT =====
                var result = new AdminDashboardDTO
                {
                    TotalListings = totalListings,
                    ActiveListings = activeListings,
                    TotalUsers = totalUsers,
                    TotalSellers = totalSellers,
                    TotalOrders = totalOrders,
                    CompletedOrders = completedOrders,
                    ProcessingOrders = processingOrders,
                    BuyerFeeRevenue = buyerFeeRevenue,
                    SellerFeeRevenue = sellerFeeRevenue,
                    InspectionRevenue = inspectionRevenue,
                    MonthlyRevenue = totalRevenue,

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

        public async Task<ResponseDTO> GetRevenueAnalyticsAsync(int months = 6)
        {
            try
            {
                var fromDate = DateTime.UtcNow.AddMonths(-months);

                var rawData = await _orderRepo.AsQueryable()
                    .Where(o =>
                        !o.IsDeleted &&
                        o.Status == OrderStatusEnum.Completed &&
                        o.CompletedAt.HasValue &&
                        o.CompletedAt.Value >= fromDate
                    )
                    .GroupBy(o => new
                    {
                        o.CompletedAt.Value.Year,
                        o.CompletedAt.Value.Month
                    })
                    .Select(g => new
                    {
                        g.Key.Year,
                        g.Key.Month,
                        gmv = g.Sum(x => x.TotalAmount)
                    })
                    .ToListAsync();

                var data = rawData
                    .Select(x => new
                    {
                        month = $"{x.Year}-{x.Month:D2}",
                        gmv = x.gmv,
                        revenue = x.gmv * 0.05m
                    })
                    .OrderBy(x => x.month)
                    .ToList();

                return Success(data);
            }
            catch (Exception ex)
            {
                return Fail("Lỗi revenue analytics: " + ex.Message);
            }
        }

        public async Task<ResponseDTO> GetListingAnalyticsAsync()
        {
            try
            {
                var query = _listingRepo.AsQueryable().Where(x => !x.IsDeleted);

                var total = await query.CountAsync();

                var pending = await query.CountAsync(x =>
                    x.Status == ListingStatusEnum.PendingInspection ||
                    x.Status == ListingStatusEnum.PendingReview);

                var approved = await query.CountAsync(x =>
                    x.Status == ListingStatusEnum.Published);

                var rejected = await query.CountAsync(x =>
                    x.Status == ListingStatusEnum.Rejected);

                var data = new
                {
                    total,
                    pending,
                    approved,
                    rejected
                };

                return Success(data);
            }
            catch (Exception ex)
            {
                return Fail("Lỗi listing analytics: " + ex.Message);
            }
        }

        public async Task<ResponseDTO> GetUserAnalyticsAsync()
        {
            try
            {
                var query = _userRepo.AsQueryable();

                var total = await query.CountAsync();
                var buyerCount = await query.CountAsync(x => x.Role == RoleEnum.BUYER);
                var sellerCount = await query.CountAsync(x => x.Role == RoleEnum.SELLER);

                var data = new
                {
                    total,
                    buyerCount,
                    sellerCount
                };

                return Success(data);
            }
            catch (Exception ex)
            {
                return Fail("Lỗi user analytics: " + ex.Message);
            }
        }
    }
}
