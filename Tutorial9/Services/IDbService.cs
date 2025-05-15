using Tutorial9.Model;

namespace Tutorial9.Services;

public interface IDbService
{
    Task<int?> AddProductToWarehouseAsync(WarehouseDTO dto);
    Task<int> AddProductToWarehouseProcedureAsync(WarehouseDTO dto);
}