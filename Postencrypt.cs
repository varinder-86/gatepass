using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System;

namespace Gatepaswebapi
{
    public class Postencrypt
    {
        public static async Task PostEncryptedData(string encryptedAadhaar, string apiUrl, string token)
        {

            using (HttpClient client = new HttpClient())
            {
                string requestId = Guid.NewGuid().ToString();
                string timestamp = DateTime.UtcNow.ToString("o");


                 
                client.DefaultRequestHeaders.Add("Request-Id", requestId);
                client.DefaultRequestHeaders.Add("Timestamp", timestamp);
                client.DefaultRequestHeaders.Add("Authorization", token); ; //X-CM-ID

                var data = new
                {

                    txnId = "",
                    scope = new[] { "abha-enrol" },
                    loginHint = "aadhaar",
                    loginId = encryptedAadhaar,
                    otpSystem = "aadhaar",
                    // Aadhaar = encryptedAadhaar

                };
                //client.DefaultRequestHeaders.Add("Authorization", Token);
                var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Data posted successfully.");
                }
                else
                {
                    Console.WriteLine($"Error posting data: {response.StatusCode}");

                }
            }
        }

    }
}
