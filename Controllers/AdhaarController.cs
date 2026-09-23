using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq.Expressions;
using Gatepaswebapi.Model;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Gatepaswebapi.Gatepaswebapi;
using static Gatepaswebapi.Model.Adhaar;
using Microsoft.AspNetCore.Http;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.VisualBasic;
using System.Reflection;
using static System.Net.WebRequestMethods;
using NuGet.Common;
using System.Xml;

namespace Gatepaswebapi.Controllers
{
    [ApiController]
    //[Route("[controller]")]
    public class AdhaarController : Controller
    {
        private static string txnid, txnidnew;
        private static string atoken , xtoken;
        private static string mobile;
        public static string fname, msg;
        // private static string atoken;


        [HttpPost]
        [Route("api/adhaar")]

        public async Task<string> GetAccessTokenAsync(string adhano)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Set the URL
                    string url = "https://dev.abdm.gov.in/api/hiecm/gateway/v3/sessions";

                    // Add required headers
                    string requestId = Guid.NewGuid().ToString();
                    string timestamp = DateTime.UtcNow.ToString("o"); // ISO 8601 format
                    string authorizationToken = "Bearer your-existing-token-if-any"; // Replace if necessary

                    client.DefaultRequestHeaders.Add("Request-Id", requestId);
                    client.DefaultRequestHeaders.Add("Timestamp", timestamp);
                    client.DefaultRequestHeaders.Add("Authorization", authorizationToken); //X-CM-ID
                    client.DefaultRequestHeaders.Add("X-CM-ID", "sbx");
                    // client.Timeout = TimeSpan.FromSeconds(60); // Example timeout

                    // Prepare the request body
                    var requestData = new
                    {
                        clientId = "SBXID_008741",
                        clientSecret = "61516ab3-a33b-4f1e-b3a8-a79362a91d9a",
                        grantType = "client_credentials"
                    };

                    string jsonBody = JsonConvert.SerializeObject(requestData);
                    var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                    // Send POST request
                    HttpResponseMessage response = await client.PostAsync(url, content);

                    // Debugging step: log response details
                    Console.WriteLine($"Response Status Code: {response.StatusCode}");
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Response Body: {responseBody}");


                    // Parse the JSON response
                    var responseData = JsonConvert.DeserializeObject<ResponseData>(responseBody);

                    // Store values into variables
                    atoken = responseData.AccessToken;
                    int expiresIn = responseData.ExpiresIn;
                    int refreshExpiresIn = responseData.RefreshExpiresIn;
                    string refreshToken = responseData.RefreshToken;
                    string tokenType = responseData.TokenType;


                    string encryptedAadhaar = AadhaarEncryptor.Encrypt(adhano);
                    string apiUrl = "https://abhasbx.abdm.gov.in/abha/api/v3/enrollment/request/otp";
                    // await CallApiWithBearerToken(accessToken, apiUrl, encryptedAadhaar);
                    txnid = await CallApiWithBearerToken(atoken, apiUrl, encryptedAadhaar);
                    //string ovalue = await OtpPost(null,accessToken);

                    if (response.IsSuccessStatusCode)
                    {
                        return responseBody;
                    }
                    else
                    {
                        return $"Error: {response.StatusCode}, Details: {responseBody}";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Exception: {ex.Message}";
            }
        }


        public class AadhaarRequest
        {
            public string AadhaarNumber { get; set; }
            public string AccessToken { get; set; }
        }

        private static async Task<string> CallApiWithBearerToken(string bearerToken, string apiUrl, string encryptedAadhaar)
        {
            using (HttpClient client = new HttpClient())
            {

                // Add Authorization header
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
                client.DefaultRequestHeaders.Add("REQUEST-ID", Guid.NewGuid().ToString());
                client.DefaultRequestHeaders.Add("TIMESTAMP", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));


                var requestBody = new
                {
                    txnId = "",
                    scope = new[] { "abha-enrol" },
                    loginHint = "aadhaar",
                    loginId = encryptedAadhaar,
                    otpSystem = "aadhaar",
                };
                HttpContent content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");


                // Make POST request
                HttpResponseMessage response = await client.PostAsync(apiUrl, content);

                // Get response
                string responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine("API Response: " + responseContent);

                var responseData = JsonConvert.DeserializeObject<ResponseData>(responseContent);

                txnid = responseData.txnId;

                Console.WriteLine("txnid :" + txnid);

                if (response.IsSuccessStatusCode)
                {

                    Console.WriteLine("API call succeeded!");

                }
                else
                {
                    Console.WriteLine($"API call failed: {response.StatusCode}");

                }
                return txnid;
            }

        }

        [HttpPost]
        [Route("api/otp")]

        public async Task<string> createAbha(string otp)
        {
            //if (string.IsNullOrEmpty(txnid) || string.IsNullOrEmpty(atoken))
            //{
            //    return BadRequest("Transaction ID or Access Token is not set.");
            //}

            using HttpClient client = new HttpClient();
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", atoken);
                client.DefaultRequestHeaders.Add("REQUEST-ID", Guid.NewGuid().ToString());
                client.DefaultRequestHeaders.Add("TIMESTAMP", DateTime.UtcNow.ToString("o"));

                string encryptedotp = AadhaarEncryptor.Encrypt(otp);


                var requestBody = new
                {
                    authData = new
                    {
                        authMethods = new[] { "otp" },

                        otp = new
                        {
                            timeStamp = DateTime.UtcNow.ToString("o"),
                            txnId = txnid,
                            otpValue = encryptedotp,
                            mobile = "9779331564"
                        }
                    },
                    consent = new
                    {
                        code = "abha-enrollment",
                        version = "1.4"
                    }
                };

                string url = "https://abhasbx.abdm.gov.in/abha/api/v3/enrollment/enrol/byAadhaar";
                HttpContent content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                HttpResponseMessage responB = await client.PostAsync(url, content);

                string responBody = await responB.Content.ReadAsStringAsync();
                Console.WriteLine($"Response Body: {responBody}");

                var responseCont = JsonConvert.DeserializeObject<ResponseCont>(responBody);
                //abha = responseCont.ABHANumber;
                // abha = responseCont.ABHAProfile.ABHANumber;
                mobile = responseCont.ABHAProfile.mobile;
                xtoken = responseCont.tokens.token;
                txnid = responseCont.txnId;
                Console.WriteLine("abha :" + mobile);
                if (responB.IsSuccessStatusCode)
                {
                    return responBody;
                    //Console.WriteLine(" SUCCEED");
                }
                else
                {
                    Console.WriteLine("FAILED");
                }

                return mobile;

            }

        }

        [HttpPost]
        [Route("api/otplogin")]

        public async Task<string> OtpSend()
        {
            //if (string.IsNullOrEmpty(txnid) || string.IsNullOrEmpty(atoken))
            //{
            //    return BadRequest("Transaction ID or Access Token is not set.");
            //}

            using HttpClient client = new HttpClient();
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", atoken);
                client.DefaultRequestHeaders.Add("REQUEST-ID", Guid.NewGuid().ToString());
                client.DefaultRequestHeaders.Add("TIMESTAMP", DateTime.UtcNow.ToString("o"));

                string encryptedmobile = AadhaarEncryptor.Encrypt(mobile);

                var requestBody = new
                {
                    txnId = txnid,
                   // scope = new[] { "abha-login", "aadhaar-verify" },
                    scope = new[] { "abha-enrol", "mobile-verify" },
                    //loginHint = "abha-number",
                    loginHint = "mobile",
                    loginId = encryptedmobile,
                    otpSystem = "abdm",

                };

              
                string url = "https://abhasbx.abdm.gov.in/abha/api/v3/enrollment/request/otp";
    
            
                HttpContent content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                HttpResponseMessage responB = await client.PostAsync(url, content);

                
                string responBody = await responB.Content.ReadAsStringAsync();
               
                Console.WriteLine(responBody);

                var responseCont= JsonConvert.DeserializeObject<ResponseCont>(responBody);
                txnidnew = responseCont.txnId;
                if (responB.IsSuccessStatusCode)
                {
                    return responBody;
                }
                else
                {
                    Console.WriteLine("FAILED");
                }

                return responBody;

            }

        }

        [HttpPost]
        [Route("api/verify")]
        public async Task<string> otpVerify(string otp)
        {
            using HttpClient client = new HttpClient();
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", atoken);
                client.DefaultRequestHeaders.Add("Request-ID",Guid.NewGuid().ToString());
                client.DefaultRequestHeaders.Add("TIMESTAMP",DateTime.UtcNow.ToString("o"));
                string encryptedotp = AadhaarEncryptor.Encrypt(otp);

                var requestBody = new
                {
                    
                    scope = new[] { "abha-enrol", "mobile-verify" },
                    authData =  new
                    {
                        authMethods = new[] { "otp" },
                                      
                      otp = new
                       {
                        timeStamp = DateTime.UtcNow.ToString("o"),
                        txnId = txnidnew,
                        otpValue = encryptedotp
                        //otpValue = string.IsNullOrEmpty(encryptedotp) ? "default-otp" : encryptedotp // Provide a default value if encryptedotp is null or empty
                        }
                    }
                };

                string url = "https://abhasbx.abdm.gov.in/abha/api/v3/enrollment/auth/byAbdm";
                HttpContent content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(requestBody),Encoding.UTF8,"application/json");

                HttpResponseMessage responMsg= await client.PostAsync(url, content);
                string msgstring = await responMsg.Content.ReadAsStringAsync();
                if (responMsg.IsSuccessStatusCode)
                {
                    return msgstring; 

                }
                else
                {
                    Console.WriteLine("failed");
                }
                return msgstring;
            }               
        }


        [HttpPost]
        [Route("api/emaillink")]

        //public async Task<IActionResult> emaillink(string email)
        //{
        //    // Get the JWT token from the session or wherever it is stored
        //   //var tkn = HttpContext.Session.GetString("JWT_Token");
        //    if (string.IsNullOrEmpty(tkn))
        //    {
        //        return Unauthorized("Token is missing or expired");
        //    }


        //    // Proceed with making the request to the external API
        //    string responseMessage = await SendEmailVerificationRequest(email, tkn);
        //    return Ok(responseMessage);
        //}
        public async Task<string> SendEmailVerificationRequest( string email)
        {
            using HttpClient client = new HttpClient();
            {

                //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", xtoken);
                client.DefaultRequestHeaders.Authorization= new AuthenticationHeaderValue("bearer", atoken);
               
                client.DefaultRequestHeaders.Add("REQUEST-ID", Guid.NewGuid().ToString());
                client.DefaultRequestHeaders.Add("TIMESTAMP", DateTime.UtcNow.ToString("o"));
                client.DefaultRequestHeaders.Add("X-token", "Bearer" + xtoken);
                string encryptedemail = AadhaarEncryptor.Encrypt(email);
                var requestBody = new
                {
                    scope = new[] { "abha-profile", "email-link-verify" },
                    loginHint="email",
                    loginId= encryptedemail,
                    otpSystem ="abdm"

                };
                string url = "https://abhasbx.abdm.gov.in/abha/api/v3/profile/account/request/emailVerificationLink";
                HttpContent content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "aaplication/json");
                HttpResponseMessage responmsg = await client.PostAsync(url, content);
                string msgstrg= await responmsg.Content.ReadAsStringAsync();
                if(responmsg.IsSuccessStatusCode)
                {
                    return msgstrg;
                }
                else
                {
                    Console.WriteLine("failed");
                }
                return msgstrg;
            }
            
        }



    }
}
        
    

  

