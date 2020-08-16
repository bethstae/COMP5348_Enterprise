using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using DeliveryCo.MessageTypes;

namespace DeliveryCo.Services.Interfaces
{
    [ServiceContract]
    public interface IDeliveryService
    {
        //changed return parameter from Guid to void as operation is one way (so will throw exception)
        [OperationContract(IsOneWay = true)]
        //[TransactionFlow(TransactionFlowOption.Allowed)]
        void SubmitDelivery(DeliveryInfo pDeliveryInfo, IDictionary<int, string> wareHouseList);
    }
}
