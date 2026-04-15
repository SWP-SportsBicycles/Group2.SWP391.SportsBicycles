using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode;
using Group2.SWP391.SportsBicycles.Common.Enums;
using Group2.SWP391.SportsBicycles.DAL.Contract;
using Group2.SWP391.SportsBicycles.DAL.Models;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Microsoft.EntityFrameworkCore;

namespace Group2.SWP391.SportsBicycles.Services.Implementation
{
    public class AdminListingService : IAdminListingService
    {
        private readonly IGenericRepository<Listing> _listingRepo;
        private readonly IGenericRepository<Bike> _bikeRepo;
        private readonly IUnitOfWork _uow;

        public AdminListingService(
            IGenericRepository<Listing> listingRepo,
            IGenericRepository<Bike> bikeRepo,
            IUnitOfWork uow)
        {
            _listingRepo = listingRepo;
            _bikeRepo = bikeRepo;
            _uow = uow;
        }

        // ================= HELPER =================
        private static ResponseDTO Success(object? data = null, BusinessCode code = BusinessCode.GET_DATA_SUCCESSFULLY)
            => new()
            {
                IsSucess = true,
                BusinessCode = code,
                Data = data
            };

        private static ResponseDTO Fail(BusinessCode code, string msg)
            => new()
            {
                IsSucess = false,
                BusinessCode = code,
                Message = msg
            };

        // ================= APPROVE =================
        public async Task<ResponseDTO> ApproveListingAsync(Guid listingId)
        {
            var listing = await _listingRepo.AsQueryable()
                .Include(l => l.Bikes)
                .FirstOrDefaultAsync(l => l.Id == listingId);

            if (listing == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy listing");

            if (listing.Status != ListingStatusEnum.PendingReview)
                return Fail(BusinessCode.INVALID_ACTION, "Listing không ở trạng thái chờ duyệt");

            var bike = listing.Bikes.FirstOrDefault();
            if (bike == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy bike");

            listing.Status = ListingStatusEnum.Published;
            bike.Status = BikeStatusEnum.Available;

            await _uow.SaveChangeAsync();

            return Success(null, BusinessCode.UPDATE_SUCESSFULLY);
        }

        // ================= REJECT =================
        public async Task<ResponseDTO> RejectListingAsync(Guid listingId, RejectListingDTO dto)
        {
            var listing = await _listingRepo.GetByExpression(l => l.Id == listingId);

            if (listing == null)
                return Fail(BusinessCode.DATA_NOT_FOUND, "Không tìm thấy listing");

            if (listing.Status != ListingStatusEnum.PendingReview)
                return Fail(BusinessCode.INVALID_ACTION, "Listing không ở trạng thái chờ duyệt");

            listing.Status = ListingStatusEnum.Rejected;

            // Nếu model Listing của bạn có RejectReason thì gán thêm ở đây
            // listing.RejectReason = dto.Reason?.Trim();

            await _uow.SaveChangeAsync();

            return Success(null, BusinessCode.UPDATE_SUCESSFULLY);
        }
    }
}