using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RCON_API.Models
{
    public class GameStatus
    {
        public Data data { get; set; }
    }
    public class List
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    public class Players
    {
        public int activePlayers { get; set; }
        public int limitPlayers { get; set; }
        public List<List> list { get; set; }
    }

    public class Data
    {
        public string version { get; set; }
        public string description { get; set; }
        public Players players { get; set; }
        public string ping { get; set; }
    }
}
