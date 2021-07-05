using BackUpCollectionDAL.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BackUpCollectionDAL.Repository
{
    public class DelayRepository
    {
        CoreDbContext context;
        public DelayRepository(CoreDbContext context)
        {
            this.context = context;
        }
        /// <summary>
        /// Получить объект Delay по имени. Если такого нет, то создаем.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Delay GetByName(string name)
        {
            var result = context.Delays.Where(s => s.Name == name).FirstOrDefault<Delay>();
            if (result != null)
            {
                return result;
            }
            else
            {
                Delay resultTmp = new Delay
                {
                    Name = name
                };
                context.Entry(resultTmp).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                context.SaveChanges();
                return context.Delays.Where(s => s.Name == name).FirstOrDefault<Delay>();
            }
        }

    }
}
