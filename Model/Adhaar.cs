using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Gatepaswebapi.Model
{
    public class Adhaar
    {
        public string adhano { get; set; }
        public string encryptedAadhaar { get; set; }

        public string otp { get; set; }
        public string abha { get; set; }
        public class ResponseData
        {
            [JsonProperty("accessToken")]
            public string AccessToken { get; set; }

            [JsonProperty("expiresIn")]
            public int ExpiresIn { get; set; }

            [JsonProperty("refreshExpiresIn")]
            public int RefreshExpiresIn { get; set; }

            [JsonProperty("refreshToken")]
            public string RefreshToken { get; set; }

            [JsonProperty("tokenType")]
            public string TokenType { get; set; }

            [JsonProperty("txnId")]
            public string txnId { get; set; }
          


        }
        public class AuthData
        {
            public List<string> AuthMethods { get; set; }
           // public OtpDetails Otp { get; set; }


        }

        public class OtpDetails
        {
            public string TimeStamp { get; set; }
            public string txnId { get; set; }
            public string OtpValue { get; set; }
            public string Mobile { get; set; }
        }

        public class Consent
        {
            public string Code { get; set; }
            public string Version { get; set; }
        }

        public class RequestBody
        {
            public AuthData AuthData { get; set; }
            public Consent Consent { get; set; }
        }
       
        public class ResponseCont
        {
            public string txnId { get; set; }
            public Tokens tokens { get; set; }
            public ABHAProfile ABHAProfile { get; set; }
           
        }
        public class Tokens
        {
            public string token { get; set; }
        }
        public class ABHAProfile
        {
            public string firstName { get; set; }
            public string middleName { get; set; }
            public string lastName { get; set; }
            public string dob { get; set; }
            public string ABHANumber { get; set; }
            public string mobile { get; set; }
 

        }
        


    }
}
