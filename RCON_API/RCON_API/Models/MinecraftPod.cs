using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RCON_API.Models
{
    public class MinecraftPod
    {
        private const string MC_STATUS_BASE_ENDPOINT = "http://mcstatus.azurewebsites.net/status/";
        private readonly string _ip;

        public MinecraftPod(string ip, string name)
        {
            _ip = ip;
            this.Name = name.Replace("-lb", "");
            this.Endpoints = new MinecraftEndpoints(ip);
            this.Status = null;
        }
        public string Name { get; set; }
        public MinecraftEndpoints Endpoints { get; }

        public GameStatus Status { get; set; }

        public async Task<MinecraftPod> GetRCON()
        {
            if (_ip == null)
            {
                return this;
            }
            RestClient client = new RestClient($"{MC_STATUS_BASE_ENDPOINT}{_ip}");
            RestRequest request = new RestRequest(Method.GET);

            var response = await client.ExecuteTaskAsync<GameStatus>(request);

            Status = response.Data;

            return this;
        }

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
