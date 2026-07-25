
using CleanArchitecture.Application.Abstraction.Sms;
using IPE.SmsIrClient.Models.Requests;
using IPE.SmsIrClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Net.Http;

namespace CleanArchitecture.Infrastructure.smsProvider
{
    public class SmsService : ISmsService
    {
        private readonly SmsIr smsIr;
        private readonly long lineNumber;
        public SmsService()
        {
            smsIr = new SmsIr("0bCU8RLSS1XK7u1endCgi88C5MYojVlw5wpXon9WQEqnFrBMQrS7pdexhVjgbBeD");
            lineNumber = 300089930147;
        }

        public async Task SendMessageToUser(string phoneNumber, string message)
        {
            var model = new SendGroupSmsViewModel(lineNumber.ToString(), message, [phoneNumber]);
            var result = await SendToSmsIr(model);

        }
        private async Task<string> SendToSmsIr(SendGroupSmsViewModel model)
        {
            var _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", "0bCU8RLSS1XK7u1endCgi88C5MYojVlw5wpXon9WQEqnFrBMQrS7pdexhVjgbBeD");
            string payload = JsonSerializer.Serialize(model);
            HttpContent content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://api.sms.ir/v1/send/bulk", content);
            var result = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode is not true)
            {
                throw new Exception(result);
            }
            return result;
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
    public class SendGroupSmsViewModel
    {
        public SendGroupSmsViewModel(string lineNumber, string message, List<string> numbers, int? date = null)
        {
            LineNumber = lineNumber;
            messageText = message;
            mobiles = numbers;
            sendDateTime = date;

        }
        public string LineNumber { get; set; }

        public string messageText { get; set; }
        public List<string> mobiles { get; set; }
        public int? sendDateTime { get; set; }
    }
}
