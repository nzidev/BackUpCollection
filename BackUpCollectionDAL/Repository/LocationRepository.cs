using BackUpCollectionDAL.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BackUpCollectionDAL.Repository
{
    public class LocationRepository
    {
        CoreDbContext context;
        public LocationRepository(CoreDbContext context)
        {
            this.context = context;
        }

        /// <summary>
        ///  Получить объект Location по имени сервера. Если такого нет, то создаем.
        /// </summary>
        /// <param name="serverName"></param>
        /// <returns></returns>
        public Location GetByServerName(string serverName)
        {
            var location = context.Locations.Where(s => s.ServerName == serverName).FirstOrDefault<Location>();
            if(location != null)
            {
                return location;
            }
            else
            {
                Location locationTmp = new Location();
                locationTmp.ServerName = serverName;
                locationTmp.Description = "";
                switch(serverName.Substring(0,5))
                {
                    case "":
                        locationTmp.Name = "";
                        break;
                    case "":
                        locationTmp.Name = "";
                        break;
                    case "":
                        locationTmp.Name = "";
                        break;
                    default:
                        locationTmp.Name = "Unknown";
                        break;
                }

                context.Entry(locationTmp).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                context.SaveChanges();
                return context.Locations.Where(s => s.ServerName == serverName).FirstOrDefault<Location>();
            }
        }

        /// <summary>
        /// List всех Location
        /// </summary>
        /// <returns></returns>
        public List<Location> GetAll()
        {
            return context.Locations.ToList();
        }
    }
}
