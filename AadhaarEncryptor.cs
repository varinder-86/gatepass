namespace Gatepaswebapi
{
    using Microsoft.AspNetCore.Http.HttpResults;
    using System;
using System.Security.Cryptography;
using System.Text;

namespace Gatepaswebapi
{

    public class AadhaarEncryptor
    {
        public static string EncryptAadhaar(string aadhaarNumber, string publicKey)
        {
            try
            {
                // Convert public key from Base64 format
                byte[] publicKeyBytes = Convert.FromBase64String(publicKey);

                using (RSA rsa = RSA.Create())
                {
                    // Import the public key
                    rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

                    // Encrypt the Aadhaar number
                    byte[] dataToEncrypt = Encoding.UTF8.GetBytes(aadhaarNumber);

                    // Encrypt with RSA/OAEP using SHA-1
                    byte[] encryptedData = rsa.Encrypt(dataToEncrypt, RSAEncryptionPadding.OaepSHA1);

                    // Convert to Base64 string
                    return Convert.ToBase64String(encryptedData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during encryption: {ex.Message}");
                return null;
            }
        }
    

    // Example Usage
      
        public static string Encrypt(string aadhaarNumber)
            {
                //string aadhaarNumber = "123456789012"; // Aadhaar number
                string publicKey = @"
-----BEGIN PUBLIC KEY-----
MIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEAstWB95C5pHLXiYW59qyO
4Xb+59KYVm9Hywbo77qETZVAyc6VIsxU+UWhd/k/YtjZibCznB+HaXWX9TVTFs9N
wgv7LRGq5uLczpZQDrU7dnGkl/urRA8p0Jv/f8T0MZdFWQgks91uFffeBmJOb58u
68ZRxSYGMPe4hb9XXKDVsgoSJaRNYviH7RgAI2QhTCwLEiMqIaUX3p1SAc178ZlN
8qHXSSGXvhDR1GKM+y2DIyJqlzfik7lD14mDY/I4lcbftib8cv7llkybtjX1Aayf
Zp4XpmIXKWv8nRM488/jOAF81Bi13paKgpjQUUuwq9tb5Qd/DChytYgBTBTJFe7i
rDFCmTIcqPr8+IMB7tXA3YXPp3z605Z6cGoYxezUm2Nz2o6oUmarDUntDhq/PnkN
ergmSeSvS8gD9DHBuJkJWZweG3xOPXiKQAUBr92mdFhJGm6fitO5jsBxgpmulxpG
0oKDy9lAOLWSqK92JMcbMNHn4wRikdI9HSiXrrI7fLhJYTbyU3I4v5ESdEsayHXu
iwO/1C8y56egzKSw44GAtEpbAkTNEEfK5H5R0QnVBIXOvfeF4tzGvmkfOO6nNXU3
o/WAdOyV3xSQ9dqLY5MEL4sJCGY1iJBIAQ452s8v0ynJG5Yq+8hNhsCVnklCzAls
IzQpnSVDUVEzv17grVAw078CAwEAAQ==
-----END PUBLIC KEY-----";

                // Replace the BEGIN and END markers with empty strings
                string cleanedKey = publicKey
                    .Replace("-----BEGIN PUBLIC KEY-----", "")
                    .Replace("-----END PUBLIC KEY-----", "")
                    .Trim();

                Console.WriteLine("Cleaned Public Key:");
                Console.WriteLine(cleanedKey);

                string encryptedAadhaar = AadhaarEncryptor.EncryptAadhaar(aadhaarNumber, cleanedKey);
            Console.WriteLine("Encrypted Aadhaar (Base64): " + encryptedAadhaar);
                return encryptedAadhaar;
        }
    }

}

}
