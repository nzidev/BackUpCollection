using BackUpCollectionDAL.DataBase;
using System;
using System.Collections.Generic;
using System.Text;

namespace BackUpCollectionDAL.Repository
{
    public class BackupLogRepository
    {
        CoreDbContext context;
        public BackupLogRepository(CoreDbContext context)
        {
            this.context = context;
        }
        /// <summary>
        /// Добавить запись в журнал посещений
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="account">Учетная запись</param>
        /// <param name="browserInfo">Информация о браузере</param>
        /// <param name="user">Пользователь</param>
        public void WriteLog(string message, string account, string browserInfo, string user)
        {
            BackupLog backupLog = new BackupLog();

            backupLog.DateTime = DateTime.UtcNow;
            backupLog.Account = account;
            backupLog.SearchText = message;
            backupLog.BrowserInfo = browserInfo;
            backupLog.Username = user;
            context.Entry(backupLog).State = Microsoft.EntityFrameworkCore.EntityState.Added;
            context.SaveChanges();
        }
    }
}
