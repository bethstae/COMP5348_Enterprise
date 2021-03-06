﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BookStore.Business.Components.Interfaces;

namespace BookStore.Business.Components
{
    public class EmailProvider : IEmailProvider
    {
        public void SendMessage(EmailMessage pMessage)
        {
            EmailMessageTransferService.EmailServiceClient lClient = new EmailMessageTransferService.EmailServiceClient();
            lClient.SendEmail
                (
                    new global::EmailService.MessageTypes.EmailMessage()
                    {
                        Message = pMessage.Message,
                        ToAddresses = pMessage.ToAddress,
                        Date = DateTime.Now
                    }
                );

            /*ExternalServiceFactory.Instance.EmailService.SendEmail
                (
                    new global::EmailService.MessageTypes.EmailMessage()
                    {
                        Message = pMessage.Message,
                        ToAddresses = pMessage.ToAddress,
                        Date = DateTime.Now
                    }
                );*/
        }
    }
}
