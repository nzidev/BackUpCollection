using BackUpCollectionDAL.Repository;
using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace BackUpCollectionDAL.Extensions
{
    /// <summary>
    /// Статический класс для работы с ActiveDirectory
    /// </summary>
    public static class ActiveDirectoryOptions
    {
        /// <summary>
        /// Имя домена. Загружается из json файла при запуске
        /// </summary>
        public static string DomainName { get; set; }
        /// <summary>
        /// Имя группы с доступом. Загружается из json файла при запуске. Пока используется только в WEB-версии
        /// </summary>
        public static string AccessGroupName { get; set; }

        /// <summary>
        /// Список групп для доступа. Загружается из json файла при запуске. Пока используется только в WEB-версии
        /// </summary>
        public static List<string> AccessGroupNames { get; set; }
        /// <summary>
        /// Возвращает текущего пользователя
        /// </summary>
        /// <returns>UserPrincipal</returns>
        public static UserPrincipal GetCurrentUser()
        {

            PrincipalContext pc = new PrincipalContext(ContextType.Domain, DomainName);
            Thread.CurrentPrincipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            var username = Thread.CurrentPrincipal.Identity.Name;
            if (pc != null)
                return UserPrincipal.FindByIdentity(pc, username);
            else
                return null;
        }

        /// <summary>
        /// Является ли пользователь членом группы
        /// </summary>
        /// <param name="account">Учетная запись пользователя</param>
        /// <returns></returns>
        public static bool IsMember(string account)
        {
            
            
            if (account == null)
                return false;
            else
            {
                account = account.Split('\\')[1];
                PrincipalContext pc = null;
                try { 
                pc = new PrincipalContext(ContextType.Domain, DomainName);
                }
                catch
                {
                    return false;
                }

            

                UserPrincipal user = UserPrincipal.FindByIdentity(pc, account);
                foreach (var AccessGroupName in AccessGroupNames)
                {
                    GroupPrincipal group = GroupPrincipal.FindByIdentity(pc, AccessGroupName);
                    if (group != null && user.IsMemberOf(group))
                        return true;
                }
                return false;
            }
        }
    }
}
