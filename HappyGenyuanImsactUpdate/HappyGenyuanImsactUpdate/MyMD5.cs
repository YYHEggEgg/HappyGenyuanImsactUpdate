using System.Security.Cryptography;
using System.Text;

namespace HappyGenyuanImsactUpdate
{
    public class MyMD5
    {
        public static string GetMD5HashFromFile(string fileName)
        {
            FileStream file = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            //MD5 md5 = new MD5CryptoServiceProvider();
            MD5 md5 = MD5.Create("MD5");
            byte[] retVal = md5.ComputeHash(file);
            file.Close();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
