using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Tutorial9.Model;

namespace Tutorial9.Services;

public class DbService : IDbService
{
    private readonly IConfiguration _configuration;
    public DbService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public async Task<int?> AddProductToWarehouseAsync(WarehouseDTO dto)
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        await connection.OpenAsync();

        DbTransaction transaction = await connection.BeginTransactionAsync();
        command.Transaction = transaction as SqlTransaction;

        // BEGIN TRANSACTION
        try
        {
            command.CommandText = "SELECT 1 FROM Product WHERE IdProduct = @IdProduct";
            command.Parameters.AddWithValue("@IdProduct", dto.IdProduct);
            var exists = await command.ExecuteScalarAsync();
            if (exists == null)
                return -1;
        
            command.Parameters.Clear();
            command.CommandText = "SELECT 1 FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
            command.Parameters.AddWithValue("@IdWarehouse", dto.IdWarehouse);
            exists = await command.ExecuteScalarAsync();
            if (exists == null)
                return -2;
            
            command.Parameters.Clear();
            command.CommandText = @"SELECT o.IdOrder, p.Price, o.FulfilledAt
                                    FROM [Order] o
                                    JOIN Product p ON o.IdProduct = p.IdProduct
                                    WHERE o.IdProduct = @IdProduct AND 
                                          o.Amount = @Amount AND 
                                          o.CreatedAt < @CreatedAt";
            command.Parameters.AddWithValue("@IdProduct", dto.IdProduct);
            command.Parameters.AddWithValue("@Amount", dto.Amount);
            command.Parameters.AddWithValue("@CreatedAt", dto.CreatedAt);
            
            int? orderId = null;
            decimal? price = null;
            DateTime? fulfilledAt = null;
            
            await using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    orderId = reader.GetInt32(0);
                    price = reader.GetDecimal(1);
                    fulfilledAt = reader.IsDBNull(2) ? null : reader.GetDateTime(2);
                }
            }
            
            if (fulfilledAt != null)
            {
                await transaction.RollbackAsync();
                return -3;
            }
            
            if (orderId == null)
            {
                await transaction.RollbackAsync();
                return -4;
            }
            
            command.Parameters.Clear();
            command.CommandText = "SELECT 1 FROM Product_Warehouse WHERE IdOrder = @IdOrder";
            command.Parameters.AddWithValue("@IdOrder", orderId);
            
            exists = await command.ExecuteScalarAsync();
            if (exists != null)
            {
                await transaction.RollbackAsync();
                return -5;
            }
            
            command.Parameters.Clear();
            command.CommandText = "UPDATE [Order] SET FulfilledAt = @FulfilledAt WHERE IdOrder = @IdOrder";
            command.Parameters.AddWithValue("@FulfilledAt", DateTime.Now);
            command.Parameters.AddWithValue("@IdOrder", orderId);
            await command.ExecuteNonQueryAsync();
            
            command.Parameters.Clear();
            command.CommandText = @"
            INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
            OUTPUT INSERTED.IdProductWarehouse
            VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, @CreatedAt)";
            command.Parameters.AddWithValue("@IdWarehouse", dto.IdWarehouse);
            command.Parameters.AddWithValue("@IdProduct", dto.IdProduct);
            command.Parameters.AddWithValue("@IdOrder", orderId);
            command.Parameters.AddWithValue("@Amount", dto.Amount);
            command.Parameters.AddWithValue("@Price", dto.Amount * price);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

            var inserted = await command.ExecuteScalarAsync();
            
            await transaction.CommitAsync();
            return Convert.ToInt32(inserted);
            
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw;
        }
        // END TRANSACTION
    }

    public async Task<int> AddProductToWarehouseProcedureAsync(WarehouseDTO dto)
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        
        command.CommandText = "AddProductToWarehouse";
        command.CommandType = CommandType.StoredProcedure;
        
        command.Parameters.AddWithValue("@IdProduct", dto.IdProduct);
        command.Parameters.AddWithValue("@IdWarehouse", dto.IdWarehouse);
        command.Parameters.AddWithValue("@Amount", dto.Amount);
        command.Parameters.AddWithValue("@CreatedAt", dto.CreatedAt);
        
        await connection.OpenAsync();
        var result = await command.ExecuteScalarAsync();

        if (result == null)
            throw new Exception("Procedura zwraca null");

        return Convert.ToInt32(result);
    }
}