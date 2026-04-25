using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.Common.Helpers;
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
    public class ReviewService : IReviewService
    {
        private readonly IGenericRepository<Review> _reviewRepo;
        private readonly IGenericRepository<Order> _orderRepo;
        private readonly IUnitOfWork _uow;

        public ReviewService(
            IGenericRepository<Review> reviewRepo,
            IGenericRepository<Order> orderRepo,
            IUnitOfWork uow)
        {
            _reviewRepo = reviewRepo;
            _orderRepo = orderRepo;
            _uow = uow;
        }

        public async Task<ResponseDTO> CreateReviewAsync(Guid userId, CreateReviewDTO dto)
        {
            ResponseDTO res = new ResponseDTO();

            try
            {
                if (userId == Guid.Empty)
                {
                    res.IsSucess = false;
                    res.BusinessCode = BusinessCode.AUTH_NOT_FOUND;
                    res.Message = "Không xác định user";
                    return res;
                }

                if (dto == null)
                {
                    res.IsSucess = false;
                    res.BusinessCode = BusinessCode.INVALID_INPUT;
                    res.Message = "Data null";
                    return res;
                }

                if (dto.Rating < 1 || dto.Rating > 5)
                {
                    res.IsSucess = false;
                    res.BusinessCode = BusinessCode.INVALID_DATA;
                    res.Message = "Rating phải từ 1 đến 5";
                    return res;
                }

                if (dto.OrderId == Guid.Empty)
                {
                    res.IsSucess = false;
                    res.BusinessCode = BusinessCode.INVALID_INPUT;
                    res.Message = "OrderId không hợp lệ";
                    return res;
                }

                if (dto.Rating < 1 || dto.Rating > 5)
                {
                    res.IsSucess = false;
                    res.BusinessCode = BusinessCode.INVALID_DATA;
                    res.Message = "Rating phải từ 1 đến 5";
                    return res;
                }

                if (!string.IsNullOrEmpty(dto.Comment) && dto.Comment.Length > 500)
                {
                    res.IsSucess = false;
                    res.BusinessCode = BusinessCode.INVALID_DATA;
                    res.Message = "Comment tối đa 500 ký tự";
                    return res;
                }


                var order = await _orderRepo.AsQueryable()
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Bike)
                            .ThenInclude(b => b.Listing)
                    .Include(o => o.Review)
                    .Include(o => o.Shipment)
                    .FirstOrDefaultAsync(o =>
                        o.Id == dto.OrderId &&
                        o.UserId == userId &&
                        !o.IsDeleted);

                if (order == null)
                {
                    res.IsSucess = false;
                    res.BusinessCode = BusinessCode.DATA_NOT_FOUND;
                    res.Message = "Không tìm thấy order";
                    return res;
                }

                if (order.Shipment == null || order.Shipment.Status != ShipmentStatusEnum.Delivered)
                {
                    res.IsSucess = false;
                    res.BusinessCode = BusinessCode.INVALID_ACTION;
                    res.Message = "Đơn chưa giao thành công";
                    return res;
                }

                if (order.Status != OrderStatusEnum.Completed)
                {
                    res.IsSucess = false;
                    res.BusinessCode = BusinessCode.INVALID_ACTION;
                    res.Message = "Order chưa hoàn thành";
                    return res;
                }

                if (order.Review != null)
                {
                    res.IsSucess = false;
                    res.BusinessCode = BusinessCode.INVALID_ACTION;
                    res.Message = "Order đã được đánh giá";
                    return res;
                }

                var review = new Review
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    Rating = dto.Rating,
                    Comment = dto.Comment
                };

                await _reviewRepo.Insert(review);

                order.Review = review;
                order.UpdatedAt = DateTimeHelper.NowVN();

                await _uow.SaveChangeAsync();

                res.IsSucess = true;
                res.BusinessCode = BusinessCode.CREATED_SUCCESSFULLY;
                res.Message = "Đánh giá thành công";
                res.Data = new
                {
                    review.Id,
                    review.Rating,
                    review.Comment,
                    review.CreatedAt
                };
            }
            catch (Exception ex)
            {
                res.IsSucess = false;
                res.BusinessCode = BusinessCode.EXCEPTION;
                res.Message = "Lỗi: " + ex.Message;
            }

            return res;
        }

        public async Task<ResponseDTO> GetMyReviewsAsync(Guid sellerId)
        {
            ResponseDTO res = new ResponseDTO();

            try
            {
                if (sellerId == Guid.Empty)
                {
                    res.IsSucess = false;
                    res.BusinessCode = BusinessCode.AUTH_NOT_FOUND;
                    res.Message = "Không xác định user";
                    return res;
                }

                var reviews = await _reviewRepo.AsQueryable()
                    .Include(r => r.Order)
                        .ThenInclude(o => o.User)
                    .Include(r => r.Order)
                        .ThenInclude(o => o.OrderItems)
                            .ThenInclude(oi => oi.Bike)
                                .ThenInclude(b => b.Listing)
                    .Where(r => r.Order.OrderItems
                        .Any(oi => oi.Bike.Listing.UserId == sellerId))
                    .Select(r => new
                    {
                        r.Id,
                        r.Rating,
                        r.Comment,
                        r.CreatedAt,
                        OrderId = r.OrderId,
                        BuyerName = r.Order.User.FullName,
                        BikeName = r.Order.OrderItems
                            .Select(oi => oi.Bike.Brand + " " + oi.Bike.Category)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                res.IsSucess = true;
                res.BusinessCode = BusinessCode.GET_DATA_SUCCESSFULLY;
                res.Message = "Lấy review thành công";
                res.Data = reviews;
            }
            catch (Exception ex)
            {
                res.IsSucess = false;
                res.BusinessCode = BusinessCode.EXCEPTION;
                res.Message = "Lỗi: " + ex.Message;
            }

            return res;
        }
    }
}
