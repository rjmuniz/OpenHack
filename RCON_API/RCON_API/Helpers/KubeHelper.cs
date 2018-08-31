using k8s;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc;
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
        private const string DefaultNamespace = "default";
        private readonly IHostingEnvironment _hostingEnvironment;

        public KubeHelper(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }
        private Stream KubeConfigStream
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

                using (IKubernetes client = new Kubernetes(config))
                {

                    var list = client.ListNamespacedService(DefaultNamespace);


                    return list.Items
                        .Where(i => i.Metadata.Name != "kubernetes")
                        .Select(i =>
                            new MinecraftPod(i.Status.LoadBalancer.Ingress[0].Ip, i.Metadata.Name))
                        .ToList();


                }
            }
        }

        internal ActionResult<MinecraftPod> AddServicePod(string podName)
        {
            throw new NotImplementedException();
            using (Stream stream = KubeConfigStream)
            {
                var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(stream);

                using (IKubernetes client = new Kubernetes(config))
                {
                    //var pod = new k8s.Models.V1Pod()
                    //{
                    //    ApiVersion= "apps/v1beta1",
                    //    Kind = "Deployment",
                    //    Metadata = new k8s.Models.V1ObjectMeta() {  Name= podName },
                    //    Spec = new k8s.Models.V1PodSpec()
                    //    {
                    //        Containers = new C
                    //    }
                    //}
                    

//spec:
//  template:
//                    metadata:
//                    labels:
//                    app: minecraft
//                spec:
//      containers:
//                    -name: minecraft
//                     image: openhackteam5.azurecr.io / minecraft - server:2.0
//        volumeMounts:
//                    -mountPath: "/data"
//          name: volume
//        ports:
//          -name: port25565
//           containerPort: 25565
//         - name: port25575
//           containerPort: 25575
//        env:
//                    -name: EULA
//                     value: "TRUE"
//      volumes:
//                    -name: volume
//                     persistentVolumeClaim:
//            claimName: azurefile
//      imagePullSecrets:
//      -name: acr - auth
                 //   client.CreateNamespacedPod(pod, DefaultNamespace);
                }
            }
        }
    }
}
