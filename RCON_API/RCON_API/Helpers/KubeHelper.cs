using k8s;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using RCON_API.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RCON_API.Helpers
{
    public class KubeHelper
    {
        private readonly IHostingEnvironment _hostingEnvironment;

        public KubeHelper(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }
        public Stream KubeConfigStream
        {
            get
            {
                return File.Open(_hostingEnvironment.ContentRootPath + "/kubeConfig", FileMode.Open);
            }
        }
        public List<MinecraftPod> GetServicePods()
        {
            using (Stream stream = KubeConfigStream)
            {
                var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(stream);

                IKubernetes client = new Kubernetes(config);


                //var list = client.ListNamespacedPod("default");

                var list = client.ListNamespacedService("default");
                return list.Items.Where(i => i.Metadata.Name != "kubernetes").Select(i => new MinecraftPod(i.Status.LoadBalancer.Ingress[0].Ip, i.Metadata.Name)).ToList();


            }

        }

    }
}
