using System.Security.Cryptography;
using System.Text;

namespace Application.Cryptography
{
    public class MD5CryptographyService : ICryptographyService
    {
        public string GetHash(string input)
        {
            MD5 md5 = new MD5CryptoServiceProvider();

            md5.ComputeHash(Encoding.ASCII.GetBytes(input));

            byte[] result = md5.Hash;

            StringBuilder output = new();
            for (int i = 0; i < result.Length; i++)
            {
                output.Append(result[i].ToString("x2"));
            }

            return output.ToString();
        }
    }
}
