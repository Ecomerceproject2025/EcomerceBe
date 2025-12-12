
using System.Threading.Tasks;
using EcomerceBE.Models;
using static EcomerceBE.Service.flashSale.FlashSaleService;
namespace EcomerceBE.Service.flashSale
{
    public interface IFlashSaleService
    {
 
        Task<bool> RemoveSaleProductAsync(int flashSaleItemId);
        Task<FlashSale> CreateOrUpdateFlashSaleAsync(int? id, FlashSaleCreateDTO dto);

        Task<List<FlashSaleDto>> GetAllFlashSalesWithItemsAsync();
    }
}
