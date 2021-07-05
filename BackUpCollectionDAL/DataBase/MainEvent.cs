using System;
using System.Collections.Generic;
using System.Text;

namespace BackUpCollectionDAL.DataBase
{
    /// <summary>
    /// Основная таблица событий
    /// </summary>
    public class MainEvent
    {
        public int MainEventId { get; set; }
        public int JobID { get; set; }
        public bool isParent { get; set; }
        public int ParrentId { get; set; }
        public int LocationId { get; set; }
        public virtual Location Location { get; set; }
        public int Type { get; set; }
        public int ClientId {get;set;}
        public virtual Client Client { get; set; }
        public string Time { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public long Size { get; set; }
        public string SizeHumanRead { get; set; }
        public long Speed { get; set; }
        public long Status { get; set; }
        public int MediaServerId { get; set; }
        public virtual MediaServer MediaServer { get; set; }
        public int MasterServerId { get; set; }
        public virtual MasterServer MasterServer { get; set; }
        public int PolicyId { get; set; }
        public virtual Policy Policy { get; set; }
        
        public int DelayId { get; set; }
        public virtual Delay Delay { get; set; }
        public int FrequencyId { get; set; }
        public virtual Frequency Frequency { get; set; }

    }
}
