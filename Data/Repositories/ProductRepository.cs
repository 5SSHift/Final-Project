using Dapper;
using Wpf.Config;
using Wpf.Models;

namespace Wpf.Data.Repositories
{
    public interface IProductRepository
    {
        Task<List<Product>> GetAllProductsAsync(CancellationToken ct = default);
        Task<Product?>      GetByIdAsync(int id);
        Task<int>           CreateAsync(Product p);
        Task<bool>          UpdateAsync(Product p);
        Task<bool>          DeleteAsync(int id);
    }

    public class ProductRepository : IProductRepository
    {
        public async Task<List<Product>> GetAllProductsAsync(CancellationToken ct = default)
        {
            await using var db = DatabaseConfig.GetConnection();
            var result = await db.QueryAsync<Product>(new CommandDefinition(
                @"SELECT Id, Name, Description, Price, Stock, Category, ImageData,
                         DiscountPercentage, IsOnOffer, CreatedAt, ModifiedDate
                  FROM Products ORDER BY ModifiedDate DESC, Id DESC",
                cancellationToken: ct));
            return result.ToList();
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            await using var db = DatabaseConfig.GetConnection();
            return await db.QueryFirstOrDefaultAsync<Product>(
                "SELECT * FROM Products WHERE Id=@Id", new { Id = id });
        }

        public async Task<int> CreateAsync(Product p)
        {
            await using var db = DatabaseConfig.GetConnection();
            return await db.QueryFirstOrDefaultAsync<int>(@"
                INSERT INTO Products
                    (Name,Description,Price,Stock,Category,ImageData,DiscountPercentage,IsOnOffer,CreatedAt,ModifiedDate)
                VALUES
                    (@Name,@Description,@Price,@Stock,@Category,@ImageData,@DiscountPercentage,@IsOnOffer,GETDATE(),GETDATE());
                SELECT SCOPE_IDENTITY();", p);
        }

        public async Task<bool> UpdateAsync(Product p)
        {
            await using var db = DatabaseConfig.GetConnection();
            var rows = await db.ExecuteAsync(@"
                UPDATE Products SET
                    Name=@Name, Description=@Description, Price=@Price, Stock=@Stock,
                    Category=@Category, ImageData=@ImageData,
                    DiscountPercentage=@DiscountPercentage, IsOnOffer=@IsOnOffer,
                    ModifiedDate=GETDATE()
                WHERE Id=@Id", p);
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using var db = DatabaseConfig.GetConnection();
            return await db.ExecuteAsync("DELETE FROM Products WHERE Id=@Id", new { Id = id }) > 0;
        }
    }
}
