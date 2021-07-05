using BackUpCollectionDAL.Extensions;
using System;

namespace CreatePassword
{
    /// <summary>
    /// Консольное приложение для создания зашифрованного вида пароля. 
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Write password for crypt");
            string unprotect = Console.ReadLine();
            Console.WriteLine("Protected password:");
            Console.WriteLine(SecurityStringManager.Protect(unprotect));
            Console.ReadLine();
        }
    }
}
