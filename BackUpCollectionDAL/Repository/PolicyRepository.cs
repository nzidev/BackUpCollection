using BackUpCollectionDAL.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BackUpCollectionDAL.Repository
{
    public class PolicyRepository
    {
        CoreDbContext context;
        public PolicyRepository(CoreDbContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Получить Policy по имени. Если нет, то создаем
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Policy GetByName(string name)
        {
            List<Policy> policies = context.Policies.Where(s => s.Name == name).ToList();

            // Повторное сравние необходимо т.к. в SQL регистр не имеет значения, а в ADO(откуда все политики берем) - имеет. Политика Oracle и oracle это разные политики.
            if (policies != null && policies.Any(x => x.Name == name))
            {
                var policy = policies.Where(x => x.Name == name).FirstOrDefault<Policy>();
                return policy;
            }
            else
            {
                Policy resultTmp = new Policy
                {
                    Name = name
                };
                context.Entry(resultTmp).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                context.SaveChanges();
                policies = context.Policies.Where(s => s.Name == name).ToList();
                var policy = policies.Where(x => x.Name == name).FirstOrDefault<Policy>();
                return policy;

            }
        }

        /// <summary>
        /// Получить Policy по ID
        /// </summary>
        /// <param name="policyId"></param>
        /// <returns></returns>
        public Policy GetById(int policyId)
        {
            return context.Policies.Single(x => x.PolicyId == policyId);
        }

        /// <summary>
        /// Получить список Policy 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Policy> GetAll()
        {
            return context.Policies;
        }
    }
}
