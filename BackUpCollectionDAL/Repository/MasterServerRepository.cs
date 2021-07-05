using BackUpCollectionDAL.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BackUpCollectionDAL.Repository
{
    public class MasterServerRepository
    {
        CoreDbContext context;
        public MasterServerRepository(CoreDbContext context)
        {
            this.context = context;
        }
        /// <summary>
        /// Получить MasterServer по ID. Если нет, то создаем
        /// </summary>
        /// <param name="id"></param>
        /// <param name="serverName"></param>
        /// <returns></returns>
        public MasterServer GetById(int id, string serverName)
        {
            var result = context.MasterServers.Where(s => s.OpsId == id && s.ServerName == serverName).FirstOrDefault<MasterServer>();
            if (result != null)
            {
                return result;
            }
            else
            {
                MasterServer resultTmp = new MasterServer
                {
                    OpsId = id,
                    ServerName = serverName
                };
                context.Entry(resultTmp).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                context.SaveChanges();
                return context.MasterServers.Where(s => s.OpsId == id && s.ServerName == serverName).FirstOrDefault<MasterServer>();
            }
        }

        /// <summary>
        /// Проверяем является ли клиент политики мастерсервером
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool isClientMaster(string Name)
        {
            if (context.MasterServers.Any(x => x.Name == Name) || context.MasterServers.Any(x => x.Name + ".ru" == Name ))
                return true;
            else
                return false;
        }
        /// <summary>
        /// Устанавливаем указаный мастерсервер
        /// </summary>
        /// <param name="serverId"></param>
        /// <param name="name"></param>
        /// <param name="serverName"></param>
        public void SetServerName(int serverId, string name,string serverName)
        {
            MasterServer masterServer = GetById(serverId, serverName);
            masterServer.Name = name;
            masterServer.ServerName = serverName;
            context.SaveChanges();
        }
    }
}
