using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RCON_API.Models
{
    public class MinecraftPod
    {
        public MinecraftPod(string ip, string name)
        {
            this.Name = name;
            this.Endpoints = new MinecraftEndpoints(ip);
        }
        public string  Name { get; set; }
        public MinecraftEndpoints Endpoints { get; }

    }
    public class MinecraftEndpoints
    {
        string _ip;
        public MinecraftEndpoints(string ip)
        {
            _ip = ip;
        }
        public string Minecraft
        {
            get { return _ip + ":25565"; }
        }
        public string RCON
        {
            get { return _ip + ":25575"; }
        }
    }

}
