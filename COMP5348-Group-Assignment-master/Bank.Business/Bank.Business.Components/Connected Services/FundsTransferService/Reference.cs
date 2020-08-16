﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Bank.Business.Components.FundsTransferService {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="FundsTransferService.IFundsTransferService")]
    public interface IFundsTransferService {
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/IFundsTransferService/TransferOutcome")]
        void TransferOutcome(bool pOutcome, string Oid);
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/IFundsTransferService/TransferOutcome")]
        System.Threading.Tasks.Task TransferOutcomeAsync(bool pOutcome, string Oid);
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/IFundsTransferService/RefundOutcome")]
        void RefundOutcome(bool pOutcome, string Oid);
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/IFundsTransferService/RefundOutcome")]
        System.Threading.Tasks.Task RefundOutcomeAsync(bool pOutcome, string Oid);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface IFundsTransferServiceChannel : Bank.Business.Components.FundsTransferService.IFundsTransferService, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class FundsTransferServiceClient : System.ServiceModel.ClientBase<Bank.Business.Components.FundsTransferService.IFundsTransferService>, Bank.Business.Components.FundsTransferService.IFundsTransferService {
        
        public FundsTransferServiceClient() {
        }
        
        public FundsTransferServiceClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public FundsTransferServiceClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public FundsTransferServiceClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public FundsTransferServiceClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public void TransferOutcome(bool pOutcome, string Oid) {
            base.Channel.TransferOutcome(pOutcome, Oid);
        }
        
        public System.Threading.Tasks.Task TransferOutcomeAsync(bool pOutcome, string Oid) {
            return base.Channel.TransferOutcomeAsync(pOutcome, Oid);
        }
        
        public void RefundOutcome(bool pOutcome, string Oid) {
            base.Channel.RefundOutcome(pOutcome, Oid);
        }
        
        public System.Threading.Tasks.Task RefundOutcomeAsync(bool pOutcome, string Oid) {
            return base.Channel.RefundOutcomeAsync(pOutcome, Oid);
        }
    }
}
