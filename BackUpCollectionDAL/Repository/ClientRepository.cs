using BackUpCollectionDAL.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BackUpCollectionDAL.Repository
{
    public class ClientRepository
    {
        CoreDbContext context;
        public ClientRepository(CoreDbContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Получить объект Client по имени
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Client GetByName(string name)
        {
            var result = context.Clients.Where(s => s.Name == name).FirstOrDefault<Client>();
            if (result != null)
            {
                return result;
            }
            else
            {
                Client resultTmp = new Client
                {
                    Name = name
                };
                context.Entry(resultTmp).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                context.SaveChanges();
                return context.Clients.Where(s => s.Name == name).FirstOrDefault<Client>();
            }
        }

    }
}
