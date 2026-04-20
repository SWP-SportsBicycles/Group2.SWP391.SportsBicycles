using Group2.SWP391.SportsBicycles.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Contract
{
    public interface ICartService
    {
        Task<ResponseDTO> AddToCartAsync(Guid userId, AddToCartDTO dto);

        Task<ResponseDTO> UpdateCartItemSelectionAsync(Guid userId, UpdateCartItemSelectionDTO dto);

        Task<ResponseDTO> GetMyCartAsync(Guid userId);

        Task<ResponseDTO> RemoveCartItemAsync(Guid userId, Guid cartItemId);

        Task<ResponseDTO> CreateOrderFromSelectedCartAsync(Guid userId, CreateOrderFromCartDTO dto);
        Task<ResponseDTO> PreviewCheckoutAsync(Guid userId, PreviewCheckoutDTO dto);

    }

}
