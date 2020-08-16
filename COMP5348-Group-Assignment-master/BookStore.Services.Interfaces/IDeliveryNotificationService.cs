using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace BookStore.Services.Interfaces
{
    public enum DeliveryInfoStatus { Submitted, OrderPicked, OrderInTransit, Delivered, Failed }

    [ServiceContract]
    public interface IDeliveryNotificationService
    {
        [OperationContract(IsOneWay = true)]
        void NotifyDeliveryCompletion(Guid pDeliverId, DeliveryInfoStatus status);
    }
}
