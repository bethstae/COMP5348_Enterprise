using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Services.Interfaces
{
    public enum TransferOutcome { Successful, Failed }

    [ServiceContract]
    public interface IFundsTransferService
    {
        [OperationContract(IsOneWay = true)]
        void TransferOutcome(bool pOutcome, string Oid);

        [OperationContract(IsOneWay = true)]
        void RefundOutcome(bool pOutcome, string Oid);
    }
}
