using BackUpCollectionDAL.DataBase;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BackUpCollectionDAL.Repository
{
    public class ProgressRepository
    {

        CoreDbContext context;
        public ProgressRepository(CoreDbContext context)
        {
            this.context = context;
        }

        /// <summary>
        ///  Установить Progress. Если нет, то создаем
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="current"></param>
        /// <param name="total"></param>
        public void SetProgress(string serviceName, int current, int total)
        {
            Progress currentProgres = context.Progresses.Where(x => x.serviceName == serviceName).FirstOrDefault<Progress>();
            if (currentProgres != null)
            {
                
                    currentProgres.CurrentPos = current;
                    currentProgres.Total = total;
               
                context.Entry(currentProgres).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            }
            else
            {
                Progress currentProgresNew = new Progress();
                currentProgresNew.serviceName = serviceName;
                currentProgresNew.CurrentPos = current;
                currentProgresNew.Total = total;
                context.Entry(currentProgresNew).State = Microsoft.EntityFrameworkCore.EntityState.Added;
            }
            context.SaveChanges();
        }

    }
}
