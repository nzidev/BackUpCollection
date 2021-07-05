using System;
using System.Collections.Generic;
using System.Text;

namespace BackUpCollectionDAL.Extensions
{
    /// <summary>
    /// Для работы с web-страницей статистики
    /// </summary>
    public class StatiscticItem
    {
        public string Name { get; set; }
        public TypeOfStatistic typeOfStatistic { get; set; }
        public int Count { get; set; }
        public long Size { get; set; }
        public string SizeHumanRead { get; set; }
    }

    public enum TypeOfStatistic
    {
        Year,
        Month
    }
}
