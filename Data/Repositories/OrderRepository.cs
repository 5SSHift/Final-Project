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
        /// <summary>
        /// Acceptă comanda: setează Status = "Processing" și scade stocul
        /// pentru fiecare produs din comandă, atomic într-o tranzacție.
        /// Returnează null la succes sau un mesaj de eroare (ex. stoc insuficient).
        /// </summary>
        Task<string?> ApproveOrderAsync(int orderId);
        /// <summary>
        /// Respinge comanda: setează Status = "Cancelled".
        /// Dacă comanda era deja "Processing", restabilește stocul.
        /// </summary>
        Task<string?> RejectOrderAsync(int orderId);
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
                ORDER BY
                    CASE o.Status
                        WHEN 'Pending'    THEN 1
                        WHEN 'Processing' THEN 2
                        WHEN 'Shipped'    THEN 3
                        WHEN 'Delivered'  THEN 4
                        WHEN 'Cancelled'  THEN 5
                        ELSE 6
                    END,
                    o.OrderID DESC");
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
                ORDER BY
                    CASE o.Status
                        WHEN 'Pending'    THEN 1
                        WHEN 'Processing' THEN 2
                        WHEN 'Shipped'    THEN 3
                        WHEN 'Delivered'  THEN 4
                        WHEN 'Cancelled'  THEN 5
                        ELSE 6
                    END,
                    o.OrderID DESC",
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

        // ── APPROVE ──────────────────────────────────────────────────────────
        public async Task<string?> ApproveOrderAsync(int orderId)
        {
            using var db = DatabaseConfig.GetConnection();
            await db.OpenAsync();
            using var tx = await db.BeginTransactionAsync();
            try
            {
                // Verificăm starea curentă a comenzii
                var currentStatus = await db.QueryFirstOrDefaultAsync<string>(
                    "SELECT Status FROM Orders WHERE OrderID = @OrderID",
                    new { OrderID = orderId }, tx);

                if (currentStatus is null)
                    return "Comanda nu a fost găsită.";

                if (currentStatus != "Pending")
                    return $"Comanda nu poate fi acceptată (status curent: {currentStatus}).";

                // Preluăm toate produsele din comandă
                var details = (await db.QueryAsync<OrderDetail>(
                    "SELECT Product_Id, Quantity FROM OrderDetails WHERE OrderID = @OrderID",
                    new { OrderID = orderId }, tx)).ToList();

                // Verificăm stocul pentru fiecare produs
                foreach (var d in details)
                {
                    var stock = await db.QueryFirstOrDefaultAsync<int>(
                        "SELECT Stock FROM Products WHERE Id = @Id",
                        new { Id = d.Product_Id }, tx);

                    if (stock < d.Quantity)
                    {
                        var name = await db.QueryFirstOrDefaultAsync<string>(
                            "SELECT Name FROM Products WHERE Id = @Id",
                            new { Id = d.Product_Id }, tx) ?? $"ID {d.Product_Id}";
                        await tx.RollbackAsync();
                        return $"Stoc insuficient pentru \"{name}\" (disponibil: {stock}, cerut: {d.Quantity}).";
                    }
                }

                // Scădem stocul fiecărui produs
                foreach (var d in details)
                {
                    await db.ExecuteAsync(
                        "UPDATE Products SET Stock = Stock - @Qty, ModifiedDate = GETDATE() WHERE Id = @Id",
                        new { Qty = d.Quantity, Id = d.Product_Id }, tx);
                }

                // Actualizăm statusul comenzii
                await db.ExecuteAsync(
                    "UPDATE Orders SET Status = 'Processing' WHERE OrderID = @OrderID",
                    new { OrderID = orderId }, tx);

                await tx.CommitAsync();
                return null; // succes
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return $"Eroare internă: {ex.Message}";
            }
        }

        // ── REJECT ───────────────────────────────────────────────────────────
        public async Task<string?> RejectOrderAsync(int orderId)
        {
            using var db = DatabaseConfig.GetConnection();
            await db.OpenAsync();
            using var tx = await db.BeginTransactionAsync();
            try
            {
                var currentStatus = await db.QueryFirstOrDefaultAsync<string>(
                    "SELECT Status FROM Orders WHERE OrderID = @OrderID",
                    new { OrderID = orderId }, tx);

                if (currentStatus is null)
                    return "Comanda nu a fost găsită.";

                if (currentStatus is "Cancelled" or "Delivered")
                    return $"Comanda nu poate fi respinsă (status curent: {currentStatus}).";

                // Dacă era deja "Processing", restabilim stocul
                if (currentStatus == "Processing")
                {
                    var details = (await db.QueryAsync<OrderDetail>(
                        "SELECT Product_Id, Quantity FROM OrderDetails WHERE OrderID = @OrderID",
                        new { OrderID = orderId }, tx)).ToList();

                    foreach (var d in details)
                    {
                        await db.ExecuteAsync(
                            "UPDATE Products SET Stock = Stock + @Qty, ModifiedDate = GETDATE() WHERE Id = @Id",
                            new { Qty = d.Quantity, Id = d.Product_Id }, tx);
                    }
                }

                await db.ExecuteAsync(
                    "UPDATE Orders SET Status = 'Cancelled' WHERE OrderID = @OrderID",
                    new { OrderID = orderId }, tx);

                await tx.CommitAsync();
                return null; // succes
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return $"Eroare internă: {ex.Message}";
            }
        }
    }
}
