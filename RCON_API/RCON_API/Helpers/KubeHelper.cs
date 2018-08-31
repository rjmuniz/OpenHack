using k8s;
using k8s.Models;
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
        private const string SuffixLoadBalancer = "-lb";
        private const string SuffixPVC = "-pvc";
        private readonly IHostingEnvironment _hostingEnvironment;

        public KubeHelper(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }
        private Stream KubeConfigStream
        {
            get
            {
                return File.Open(_hostingEnvironment.ContentRootPath + "/kubeConfig", FileMode.Open, FileAccess.Read);
            }
        }
        public async Task<List<MinecraftPod>> GetServicePods()
        {
            using (Stream stream = KubeConfigStream)
            {
                var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(stream);

                using (IKubernetes client = new Kubernetes(config))
                {

                    var list = client.ListNamespacedService(DefaultNamespace);


                    var podsList = list.Items
                        .Where(i => i.Metadata.Name != "kubernetes")
                        .Select(i =>
                            new MinecraftPod(i.Status.LoadBalancer.Ingress[0].Ip, i.Metadata.Name))
                        .ToList();

                    List<Task> tasks = new List<Task>();
                    foreach (var pod in podsList)
                    {
                        tasks.Add(pod.GetRCON());
                    }
                    await Task.WhenAll(tasks.ToArray());
                    return podsList;
                }
            }
        }

        internal void DeleteServicePod(string podName)
        {
            using (Stream stream = KubeConfigStream)
            {
                var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(stream);

                using (IKubernetes client = new Kubernetes(config))
                {
                    V1DeleteOptions options = new V1DeleteOptions() { };
                    client.DeleteNamespacedDeployment1(options, podName, DefaultNamespace);
                }
            }
        }

        internal void AddServicePod(string podName)
        {
            podName = podName.ToLower();
            var appLabel = new Dictionary<string, string> { { "app", podName } };
            using (Stream stream = KubeConfigStream)
            {
                var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(stream);

                using (IKubernetes client = new Kubernetes(config))
                {
                    var deployment = new Appsv1beta1Deployment("apps/v1beta1", "Deployment")
                    {
                        Metadata = new V1ObjectMeta() { Name = podName },
                        Spec = new Appsv1beta1DeploymentSpec()
                        {
                            Template = new V1PodTemplateSpec()
                            {
                                Metadata = new V1ObjectMeta() { Labels = appLabel },
                                Spec = new V1PodSpec()
                                {
                                    Containers = new k8s.Models.V1Container[]{
                                   new V1Container
                                       {
                                           Name = podName,
                                           Image = "openhackteam5.azurecr.io/minecraft-server:2.0",
                                           VolumeMounts = new V1VolumeMount[]
                                           {
                                               new k8s.Models.V1VolumeMount
                                               {
                                                    Name= "volume",
                                                    MountPath= "/data"
                                               }
                                           },
                                           Ports =  new V1ContainerPort[]
                                           {
                                               new V1ContainerPort(25565, name:"port25565"),
                                               new V1ContainerPort(25575, name:"port25575")
                                           },
                                           Env = new V1EnvVar[]{new V1EnvVar("EULA",true.ToString())}
                                       }
                                   },
                                    Volumes = new V1Volume[]
                                    {
                                        new V1Volume("volume",persistentVolumeClaim: new V1PersistentVolumeClaimVolumeSource(podName + SuffixPVC))
                                    },
                                    ImagePullSecrets = new V1LocalObjectReference[] { new V1LocalObjectReference("acr-auth") }

                                }

                            }
                        }
                    };
                    var loadBalancer = new V1Service("v1", "Service")
                    {
                        Metadata = new V1ObjectMeta { Name = podName + SuffixLoadBalancer },
                        Spec = new V1ServiceSpec
                        {
                            Type = "LoadBalancer",
                            Ports = new V1ServicePort[]{
                                  new   V1ServicePort(25565,"port25565",targetPort: 25565),
                                  new   V1ServicePort(25575,"port25575",targetPort: 25575)
                              },
                            Selector = appLabel
                        }
                    };


                    var persistentVolumeClaim = new V1PersistentVolumeClaim()
                    {
                        Metadata = new V1ObjectMeta() { Name = podName + SuffixPVC, NamespaceProperty = DefaultNamespace },
                        Spec = new V1PersistentVolumeClaimSpec
                        {
                            AccessModes = new string[] { "ReadWriteMany" },
                            StorageClassName = "azurefile",
                            Resources = new V1ResourceRequirements(requests: new Dictionary<string, ResourceQuantity> { { "storage", new ResourceQuantity("5Gi") } })
                        },
                        Status = new V1PersistentVolumeClaimStatus()
                    };
                    var pvcs = client.ListDeploymentForAllNamespaces1();
                    client.CreateNamespacedPersistentVolumeClaim(persistentVolumeClaim, DefaultNamespace);
                    client.CreateNamespacedDeployment1(deployment, DefaultNamespace);
                    client.CreateNamespacedService(loadBalancer, DefaultNamespace);
                }
            }

        }
    }
}
