using BackUpCollectionDAL.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BackUpCollectionDAL.Repository
{
    public class MediaServerRepository
    {
        CoreDbContext context;
        public MediaServerRepository(CoreDbContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Получить MediaServer по имени. Если нет, то создаем
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public MediaServer GetByName(string name)
        {
            var result = context.MediaServers.Where(s => s.Name == name).FirstOrDefault<MediaServer>();
            if (result != null)
            {
                return result;
            }
            else
            {
                MediaServer resultTmp = new MediaServer
                {
                    Name = name
                };
                context.Entry(resultTmp).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                context.SaveChanges();
                return context.MediaServers.Where(s => s.Name == name).FirstOrDefault<MediaServer>();
            }
        }

        /// <summary>
        /// Существует такой или нет. По имени.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool isContain(string name)
        {
            return context.MediaServers.Any(x => x.Name == name);
        }

        
    }
}
