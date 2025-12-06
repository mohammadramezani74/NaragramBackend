
using CleanArchitecture.Application.Abstraction.Sms;
using IPE.SmsIrClient.Models.Requests;
using IPE.SmsIrClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.smsProvider
{
    public class SmsService : ISmsService
    {
        private readonly SmsIr smsIr;
        public SmsService()
        {
            smsIr = new SmsIr("0bCU8RLSS1XK7u1endCgi88C5MYojVlw5wpXon9WQEqnFrBMQrS7pdexhVjgbBeD");
        }

        public async Task SendVerificationCode(string phoneNumber, string code)
        {
            try
            {
                await smsIr.VerifySendAsync(phoneNumber, 100000, new VerifySendParameter[] { new("Code", code) });
            }
            catch (Exception ex)
            {

                throw;
            }
                
        }
    }
}
