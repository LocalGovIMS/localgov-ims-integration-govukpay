using Application.Clients.LocalGovImsPaymentApi;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Clients.LocalGovImsPaymentApi
{
    public class LocalGovImsPaymentApiClient : ILocalGovImsPaymentApiClient
    {
        private readonly string _apiUrl;

        public LocalGovImsPaymentApiClient(IConfiguration configuration)
        {
            _apiUrl = configuration.GetValue<string>("LocalGovImsApiUrl");
        }

        public async Task<List<PendingTransactionModel>> GetPendingTransactions(string reference)
        {
            using var client = new HttpClient();

            client.BaseAddress = new Uri(_apiUrl);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage result = await client.GetAsync($"api/PendingTransactions/{reference}");

            if (result.IsSuccessStatusCode)
            {
                var response = result.Content.ReadAsStringAsync().Result;

                return JsonConvert.DeserializeObject<List<PendingTransactionModel>>(response);
            }

            return new List<PendingTransactionModel>();
        }

        public async Task<List<ProcessedTransactionModel>> GetProcessedTransactions(string reference)
        {
            using var client = new HttpClient();

            client.BaseAddress = new Uri(_apiUrl);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage result = await client.GetAsync($"api/ProcessedTransactions/{reference}");

            if (result.IsSuccessStatusCode)
            {
                var response = result.Content.ReadAsStringAsync().Result;

                return JsonConvert.DeserializeObject<List<ProcessedTransactionModel>>(response);
            }

            return new List<ProcessedTransactionModel>();
        }

        public async Task<MethodOfPaymentModel> GetCardSelfServiceMopCode()
        {
            return await GetMopByType("IsACardSelfServicePayment");
        }

        private async Task<MethodOfPaymentModel> GetMopByType(string type)
        {
            using var client = new HttpClient();

            client.BaseAddress = new Uri(_apiUrl);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage result = await client.GetAsync($"api/MethodOfPayments/?type={type}");

            if (result.IsSuccessStatusCode)
            {
                var response = result.Content.ReadAsStringAsync().Result;

                return JsonConvert.DeserializeObject<List<MethodOfPaymentModel>>(response).FirstOrDefault();
            }

            return null;
        }

        public async Task<ProcessPaymentResponseModel> ProcessPayment(string reference, ProcessPaymentModel model)
        {
            using var client = new HttpClient();

            client.BaseAddress = new Uri(_apiUrl);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpContent content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

            HttpResponseMessage result = await client.PostAsync($"api/PendingTransaction/{reference}/ProcessPayment", content);

            if (result.IsSuccessStatusCode)
            {
                var response = result.Content.ReadAsStringAsync().Result;

                return JsonConvert.DeserializeObject<ProcessPaymentResponseModel>(response);
            }

            return null;
        }

        public async Task<HttpStatusCode> Notify(NotificationModel model)
        {
            using var client = new HttpClient();

            client.BaseAddress = new Uri(_apiUrl);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpContent content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

            HttpResponseMessage result = await client.PostAsync($"api/Notification", content);

            return result.StatusCode;
        }
    }
}
