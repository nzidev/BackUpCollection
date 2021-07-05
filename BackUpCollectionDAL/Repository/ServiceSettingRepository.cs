using BackUpCollectionDAL.DataBase;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BackUpCollectionDAL.Repository
{
    public class ServiceSettingRepository
    {
        CoreDbContext context;
        public ServiceSettingRepository(CoreDbContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Добавить запись
        /// </summary>
        /// <param name="serviceSetting"></param>
        public void Add(ServiceSetting serviceSetting)
        {
            context.Entry(serviceSetting).State = Microsoft.EntityFrameworkCore.EntityState.Added;
            context.SaveChanges();
        }

        //public Dictionary<string, string> GetADOStringDic(ServiceSetting serviceSetting)
        //{
        //    return context.ServiceSettings.Include("ADOConnectionString")
        //        .Where(s => s.ServiceName == serviceSetting.ServiceName).ToDictionary(c => c.ADOConnectionString.Name, c => c.ADOConnectionString.ConnectionString);
        //}

        /// <summary>
        /// Получить список ServiceSetting по имени.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public List<ServiceSetting> GetByName(string serviceName)
        {
            var result = context.ServiceSettings
                .Include("ADOConnectionString")
                .Include("MailSetting")
                .Where(s => s.ServiceName == serviceName).ToList();
            if (result != null)
            {
                return result;
            }
            else
                return null;
        }

        /// <summary>
        /// Удаляем все записи
        /// </summary>
        public void Clear()
        {
            context.ServiceSettings.RemoveRange(context.ServiceSettings);
            context.SaveChanges();
        }
    }
}
