using BackUpCollectionDAL.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BackUpCollectionDAL.Repository
{
    public class FrequencyRepository
    {
        CoreDbContext context;
        public FrequencyRepository(CoreDbContext context)
        {
            this.context = context;
        }
        /// <summary>
        /// Получить объект Frequency по имени. Если такого нет, то создаем.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Frequency GetByName(string name)
        {
            var result = context.Frequencies.Where(s => s.Name == name).FirstOrDefault<Frequency>();
            if (result != null)
            {
                return result;
            }
            else
            {
                Frequency resultTmp = new Frequency
                {
                    Name = name
                };
                context.Entry(resultTmp).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                context.SaveChanges();
                return context.Frequencies.Where(s => s.Name == name).FirstOrDefault<Frequency>();
            }
        }
    }
}
