using k8s;
using k8s.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Rest;
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
                        {
                            try
                            {
                                string ip = (
                                    i.Status.LoadBalancer.Ingress != null && 
                                    i.Status.LoadBalancer.Ingress[0] != null 
                                        ? i.Status.LoadBalancer.Ingress[0].Ip 
                                        : null);
                                return new MinecraftPod(ip, i.Metadata.Name);
                            } catch (Exception e)
                            {
                                return new MinecraftPod(null, i.Metadata.Name);
                            }
                            
                        })
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
                    IList<V1Pod> podsList = client.ListNamespacedPod(DefaultNamespace).Items;
                    IList<V1beta1ReplicaSet> replicaSetsList = client.ListNamespacedReplicaSet2(DefaultNamespace).Items;
                    List<string> replicaSetDeleteList = new List<string>();
                    List<string> podsDeleteList = new List<string>();
                    List<string> pvcDeleteList = new List<string>();
                    V1Pod pod = null;

                    foreach (V1Pod podItem in podsList)
                    {
                        string containerName = podItem.Spec.Containers[0].Name;
                        if (containerName == podName)
                        {
                            var volumes = podItem.Spec.Volumes;
                            foreach (var replicaSet in replicaSetsList)
                            {
                                if (replicaSet.Spec.Selector.MatchLabels["app"] == podName)
                                {
                                    replicaSetDeleteList.Add(replicaSet.Metadata.Name);
                                }
                            }
                            foreach (var volume in volumes)
                            {
                                if (volume.PersistentVolumeClaim != null)
                                {
                                    pvcDeleteList.Add(volume.PersistentVolumeClaim.ClaimName);
                                }
                            }
                            podsDeleteList.Add(podItem.Metadata.Name);
                        }
                    }

                    try
                    {
                        V1Status status = client.DeleteNamespacedDeployment1(options, podName, DefaultNamespace);
                    }
                    catch (HttpOperationException e)
                    {

                    }

                    foreach (string replicaSetName in replicaSetDeleteList)
                    {
                        try
                        {
                            client.DeleteNamespacedReplicaSet2(options, replicaSetName, DefaultNamespace);
                        } catch(HttpOperationException e)
                        {

                        }
                    }
                    foreach(string podNameToDelete in podsDeleteList)
                    {
                        try
                        {
                            client.DeleteNamespacedPod(options, podNameToDelete, DefaultNamespace);
                        }
                        catch (HttpOperationException e)
                        {

                        }
                    }

                    foreach (string pvcName in pvcDeleteList)
                    {
                        try
                        {
                            client.DeleteNamespacedPersistentVolumeClaim(options, pvcName, DefaultNamespace);
                        }
                        catch (HttpOperationException e)
                        {

                        }
                    }

                    try
                    {
                        client.DeleteNamespacedService(options, $"{podName}-lb", DefaultNamespace);
                    }
                    catch (HttpOperationException e)
                    {

                    }
                    

                    options = null;
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
