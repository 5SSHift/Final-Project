using Dapper;
using Wpf.Config;
using Wpf.Models;

namespace Wpf.Data.Repositories
{
    public interface IOrderRepository
    {
        Task<List<Order>>       GetAllOrdersAsync();
        Task<List<Order>>       GetOrdersByClientAsync(int clientId);
        Task<List<OrderDetail>> GetOrderDetailsAsync(int orderId);
        Task<int>               CreateOrderAsync(Order order, List<OrderDetail> details);
        Task<bool>              UpdateStatusAsync(int orderId, string status);
    }

    public class OrderRepository : IOrderRepository
    {
        public async Task<List<Order>> GetAllOrdersAsync()
        {
            using var db = DatabaseConfig.GetConnection();
            var result = await db.QueryAsync<Order>(@"
                SELECT o.*, u.Username AS ClientUsername
                FROM Orders o
                LEFT JOIN Users u ON u.Id = o.Client_Id
                ORDER BY o.OrderID DESC");
            return result.ToList();
        }

        public async Task<List<Order>> GetOrdersByClientAsync(int clientId)
        {
            using var db = DatabaseConfig.GetConnection();
            var result = await db.QueryAsync<Order>(@"
                SELECT o.*, u.Username AS ClientUsername
                FROM Orders o
                LEFT JOIN Users u ON u.Id = o.Client_Id
                WHERE o.Client_Id = @ClientId
                ORDER BY o.OrderID DESC",
                new { ClientId = clientId });
            return result.ToList();
        }

        public async Task<List<OrderDetail>> GetOrderDetailsAsync(int orderId)
        {
            using var db = DatabaseConfig.GetConnection();
            var result = await db.QueryAsync<OrderDetail>(@"
                SELECT od.*, p.Name AS ProductName
                FROM OrderDetails od
                LEFT JOIN Products p ON p.Id = od.Product_Id
                WHERE od.OrderID = @OrderID",
                new { OrderID = orderId });
            return result.ToList();
        }

        public async Task<int> CreateOrderAsync(Order order, List<OrderDetail> details)
        {
            using var db = DatabaseConfig.GetConnection();
            await db.OpenAsync();
            using var tx = await db.BeginTransactionAsync();
            try
            {
                var orderId = await db.QueryFirstOrDefaultAsync<int>(@"
                    INSERT INTO Orders (Client_Id, OrderDate, Status, TotalAmount, ShippingAddress, PaymentMethod, CreatedAt)
                    VALUES (@Client_Id, GETDATE(), @Status, @TotalAmount, @ShippingAddress, @PaymentMethod, GETDATE());
                    SELECT SCOPE_IDENTITY();",
                    order, tx);

                foreach (var d in details)
                {
                    d.OrderID = orderId;
                    await db.ExecuteAsync(@"
                        INSERT INTO OrderDetails (OrderID, Product_Id, Quantity, UnitPrice)
                        VALUES (@OrderID, @Product_Id, @Quantity, @UnitPrice)",
                        d, tx);
                }

                await tx.CommitAsync();
                return orderId;
            }
            catch { await tx.RollbackAsync(); throw; }
        }

        public async Task<bool> UpdateStatusAsync(int orderId, string status)
        {
            using var db = DatabaseConfig.GetConnection();
            var rows = await db.ExecuteAsync(
                "UPDATE Orders SET Status=@Status WHERE OrderID=@OrderID",
                new { Status = status, OrderID = orderId });
            return rows > 0;
        }
    }
}
