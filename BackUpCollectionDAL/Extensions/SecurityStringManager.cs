using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BackUpCollectionDAL.Extensions
{
    /// <summary>
    /// Обработка строки.
    /// </summary>
    public class SecurityStringManager
    {
        readonly static Encoding _encoding = Encoding.Unicode;


        /// <summary>
        /// Расшифровка строки
        /// </summary>
        /// <param name="encryptedString">Зашифрованная строка</param>
        /// <returns></returns>
        public static string Unprotect(string encryptedString)
        {
            var protectedData = Convert.FromBase64String(encryptedString);
            var uprotectedData = ProtectedData.Unprotect(protectedData, null, DataProtectionScope.CurrentUser);

            return _encoding.GetString(uprotectedData);
        }
        /// <summary>
        /// Зашифровка строки
        /// </summary>
        /// <param name="unprotectedString">Исходная строка</param>
        /// <returns></returns>
        public static string Protect(string unprotectedString)
        {
            var uprotectedData = _encoding.GetBytes(unprotectedString);
            var protectedData = ProtectedData.Protect(uprotectedData, null,  DataProtectionScope.CurrentUser);


            return Convert.ToBase64String(protectedData);
        }
    }
}
