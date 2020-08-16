using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace BookStore.Business.Entities
{
    public partial class Order
    {

        public void CheckStockLevels()
        {
            using (BookStoreEntityModelContainer lContainer = new BookStoreEntityModelContainer())
            {
                List<OrderItem> orderItemCpy = this.OrderItems.ToList();
                int checkSum = 0;
                int prevChecksum = -1;
                int total = 0;

                foreach (OrderItem item in orderItemCpy)
                {
                    total += item.Quantity;
                }
                
                while (total > 0)
                {

                    checkSum = 0;

                    foreach (OrderItem thing in orderItemCpy) 
                    {
                        checkSum += thing.Quantity;
                    }

                    if (prevChecksum == checkSum)
                    {
                        throw new Exception("Cannot place an order - This book is out of stock");
                    }

                    prevChecksum = checkSum;

                    List<Warehouse> warehouseList = lContainer.Database.SqlQuery<Warehouse>("SELECT * FROM Warehouses").ToList();

                    int[] warehousePriority = new int[warehouseList.Count()];

                    foreach (Warehouse warehouse in warehouseList)
                    {
                        if (!lContainer.Database.SqlQuery<int>("SELECT Warehouses_Id FROM OrderWarehouse WHERE Orders_Id = @orderId AND Warehouses_Id = @warehouseId", new SqlParameter("@orderId", this.Id), new SqlParameter("@warehouseId", warehouse.Id)).ToList().Contains(warehouse.Id)) {
                            for (int i = 0; i < orderItemCpy.Count(); i++)
                            {

                                List<Stock> stocksList = lContainer.Database.SqlQuery<Stock>("SELECT * FROM Stocks WHERE Book_Id = @bookId AND WarehouseId = @warehouseId", new SqlParameter("@bookId", orderItemCpy[i].Book.Id), new SqlParameter("@warehouseId", warehouse.Id)).ToList();

                                foreach (Stock stock in stocksList)
                                {
                                    warehousePriority[warehouse.Id - 1] += (int)stock.Quantity;
                                }
                            }
                        }
                        
                    }

                    int bestWarehouse = 0;
                    int mostAvailable = 0;
                    for (int j = 0; j < warehousePriority.Length; j++)
                    {
                        if (warehousePriority[j] > mostAvailable)
                        {
                            mostAvailable = warehousePriority[j];
                            bestWarehouse = j + 1;
                        }
                    }

                    if (bestWarehouse != 0)
                    {
                        lContainer.Database.ExecuteSqlCommand("INSERT INTO OrderWarehouse VALUES(@order_Id, @warehouse_Id)", new SqlParameter("@order_Id", this.Id), new SqlParameter("@warehouse_Id", bestWarehouse));

                        foreach (OrderItem lItem in orderItemCpy)
                        {
                            List<Stock> stockList = lContainer.Database.SqlQuery<Stock>("SELECT * FROM Stocks WHERE Book_Id = @bookId AND WarehouseId = @warehouseId", new SqlParameter("@bookId", lItem.Book.Id), new SqlParameter("@warehouseId", bestWarehouse)).ToList();


                            foreach (Stock stock in stockList)
                            {
                                if (stock.Quantity > lItem.Quantity)
                                {
                                    total -= lItem.Quantity;
                                    lItem.Quantity = 0;
                                }
                                else
                                {
                                    total -= (int)stock.Quantity;
                                    lItem.Quantity -= (int)stock.Quantity;
                                }
                            }

                            

                            

                        }
                    }
                }
            }
        }

        public void UpdateStockLevels()
        {
            using (BookStoreEntityModelContainer lContainer = new BookStoreEntityModelContainer())
            {

                List<OrderItem> orderItemCpy = this.OrderItems.ToList();

                List<int> warehouseIdList = lContainer.Database.SqlQuery<int>("SELECT Warehouses_Id FROM OrderWarehouse WHERE Orders_Id = @Id", new SqlParameter("@Id", this.Id)).ToList();

                foreach (int warehouseId in warehouseIdList)
                {
                    for (int i = 0; i < orderItemCpy.Count(); i++)
                    {
                        OrderItem lItem = orderItemCpy[i];
                        List<Stock> stockList = lContainer.Database.SqlQuery<Stock>("SELECT * FROM Stocks WHERE Book_Id = @bookId AND WarehouseId = @warehouseId", new SqlParameter("@bookId", lItem.Book.Id), new SqlParameter("@warehouseId", warehouseId)).ToList();
                        int newQuantity;
                        foreach (Stock stock in stockList)
                        {
                            if ((int)stock.Quantity < lItem.Quantity)
                            {
                                newQuantity = 0;
                            }
                            else
                            {
                                newQuantity = (int)stock.Quantity - lItem.Quantity;
                            }

                            lContainer.Database.ExecuteSqlCommand("UPDATE Stocks SET Quantity = @quantity WHERE Id = @id", new SqlParameter("@id", stock.Id), new SqlParameter("@quantity", newQuantity));
                        }
                        
                    }
                }
            }
        }

        public void RollbackStockUpdates()
        {
            using (BookStoreEntityModelContainer lContainer = new BookStoreEntityModelContainer())
            {

                List<OrderItem> orderItemCpy = this.OrderItems.ToList();

                List<int> warehouseIdList = lContainer.Database.SqlQuery<int>("SELECT Warehouses_Id FROM OrderWarehouse WHERE Orders_Id = @Id", new SqlParameter("@Id", this.Id)).ToList();

                foreach (int warehouseId in warehouseIdList)
                {
                    for (int i = 0; i < orderItemCpy.Count(); i++)
                    {
                        OrderItem lItem = orderItemCpy[i];
                        List<Stock> stockList = lContainer.Database.SqlQuery<Stock>("SELECT * FROM Stocks WHERE Book_Id = @bookId AND WarehouseId = @warehouseId", new SqlParameter("@bookId", lItem.Book.Id), new SqlParameter("@warehouseId", warehouseId)).ToList();
                        int newQuantity;
                        foreach (Stock stock in stockList)
                        {
                            if ((int)stock.Quantity < lItem.Quantity)
                            {
                                newQuantity = 0;
                            }
                            else
                            {
                                newQuantity = (int)stock.Quantity + lItem.Quantity;
                            }

                            lContainer.Database.ExecuteSqlCommand("UPDATE Stocks SET Quantity = @quantity WHERE Id = @id", new SqlParameter("@id", stock.Id), new SqlParameter("@quantity", newQuantity));
                        }

                    }
                }
            }
        }
    }
}
