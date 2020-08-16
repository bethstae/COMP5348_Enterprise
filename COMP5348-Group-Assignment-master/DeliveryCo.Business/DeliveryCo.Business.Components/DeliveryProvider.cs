using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DeliveryCo.Business.Components.Interfaces;
using System.Transactions;
using DeliveryCo.Business.Entities;
using System.Threading;
using DeliveryCo.Business.Components.DeliveryNotificationService;

namespace DeliveryCo.Business.Components
{
    public class DeliveryProvider : IDeliveryProvider
    {
        public void SubmitDelivery(DeliveryCo.Business.Entities.DeliveryInfo pDeliveryInfo, IDictionary<int, string> wareHouseList)
        {
            using(TransactionScope lScope = new TransactionScope())
            using(DeliveryCoEntityModelContainer lContainer = new DeliveryCoEntityModelContainer())
            {
                //pDeliveryInfo.DeliveryIdentifier = Guid.NewGuid();
                pDeliveryInfo.Status = 0;
                lContainer.DeliveryInfo.Add(pDeliveryInfo);
                lContainer.SaveChanges();
                Console.WriteLine("Delivery Request received from Bookstore : Delivery ID " + pDeliveryInfo.DeliveryIdentifier);
                DeliveryNotificationService.DeliveryNotificationServiceClient lClient = new DeliveryNotificationService.DeliveryNotificationServiceClient();
                lClient.NotifyDeliveryCompletion(pDeliveryInfo.DeliveryIdentifier, DeliveryInfoStatus.Submitted);
                ThreadPool.QueueUserWorkItem(new WaitCallback((pObj) => ScheduleDelivery(pDeliveryInfo, wareHouseList)));
                lScope.Complete();
            }
            //return pDeliveryInfo.DeliveryIdentifier;
        }

        private void ScheduleDelivery(DeliveryInfo pDeliveryInfo, IDictionary<int, string> wareHouseList)
        {
            //Console.WriteLine("Delivering to" + pDeliveryInfo.DestinationAddress);
            Thread.Sleep(3000);
            //notifying of delivery pickup
            foreach (KeyValuePair<int, string> kvp in wareHouseList)
            {
                Console.WriteLine("Items for delivery " + pDeliveryInfo.DeliveryIdentifier + " picked up from Warehouse: " + kvp.Value);
            }
            //Console.WriteLine("Items for delivery " +pDeliveryInfo.DeliveryIdentifier+ " picked up from warehouse/s");
            using (TransactionScope lScope = new TransactionScope())
            using (DeliveryCoEntityModelContainer lContainer = new DeliveryCoEntityModelContainer())
            {
                pDeliveryInfo.Status = 1;
                lContainer.SaveChanges();
                DeliveryNotificationService.DeliveryNotificationServiceClient lClient = new DeliveryNotificationService.DeliveryNotificationServiceClient();
                lClient.NotifyDeliveryCompletion(pDeliveryInfo.DeliveryIdentifier, DeliveryInfoStatus.OrderPicked);
                //IDeliveryNotificationService lService = DeliveryNotificationServiceFactory.GetDeliveryNotificationService(pDeliveryInfo.DeliveryNotificationAddress);
                //lService.NotifyDeliveryCompletion(pDeliveryInfo.DeliveryIdentifier, DeliveryInfoStatus.Delivered);
                lScope.Complete();
            }

            Thread.Sleep(3000);
            //notifying of delivery transit
            Console.WriteLine("Delivery " + pDeliveryInfo.DeliveryIdentifier + " is in transit to destination " + pDeliveryInfo.DestinationAddress);
            using (TransactionScope lScope = new TransactionScope())
            using (DeliveryCoEntityModelContainer lContainer = new DeliveryCoEntityModelContainer())
            {
                pDeliveryInfo.Status = 2;
                lContainer.SaveChanges();
                DeliveryNotificationService.DeliveryNotificationServiceClient lClient = new DeliveryNotificationService.DeliveryNotificationServiceClient();
                lClient.NotifyDeliveryCompletion(pDeliveryInfo.DeliveryIdentifier, DeliveryInfoStatus.OrderInTransit);
                lScope.Complete();
            }

            Thread.Sleep(3000);
            //notifying of delivery completion
            Console.WriteLine("Order " + pDeliveryInfo.OrderNumber + " has been delivered to " + pDeliveryInfo.DestinationAddress);
            using (TransactionScope lScope = new TransactionScope())
            using (DeliveryCoEntityModelContainer lContainer = new DeliveryCoEntityModelContainer())
            {
                pDeliveryInfo.Status = 1;
                lContainer.SaveChanges();
                DeliveryNotificationService.DeliveryNotificationServiceClient lClient = new DeliveryNotificationService.DeliveryNotificationServiceClient();
                lClient.NotifyDeliveryCompletion(pDeliveryInfo.DeliveryIdentifier, DeliveryInfoStatus.Delivered);
                lScope.Complete();
            }


        }
    }
}
