using MME.Domain.Common.Options;
using MME.Domain.Utils;
using MME.Repositories.Demo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using SqlSugar;
using System.Reflection;
using System;

namespace MME.Domain.Common.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 从程序集中加载类型并添加到容器中
        /// </summary>
        /// <param name="services">容器</param>
        /// <param name="assemblies">程序集集合</param>
        /// <returns></returns>
        public static IServiceCollection AddServicesFromAssemblies(this IServiceCollection services, params string[] assemblies)
        {
            Type attributeType = typeof(ServiceDescriptionAttribute);
            //var refAssembyNames = Assembly.GetExecutingAssembly().GetReferencedAssemblies();
            foreach (var item in assemblies)
            {
                Assembly assembly = Assembly.Load(item);

                var types = assembly.GetTypes();

                foreach (var classType in types)
                {
                    if (!classType.IsAbstract && classType.IsClass && classType.IsDefined(attributeType, false))
                    {
                        ServiceDescriptionAttribute serviceAttribute = (classType.GetCustomAttribute(attributeType) as ServiceDescriptionAttribute);
                        switch (serviceAttribute.Lifetime)
                        {
                            case ServiceLifetime.Scoped:
                                services.AddScoped(serviceAttribute.ServiceType, classType);
                                break;

                            case ServiceLifetime.Singleton:
                                services.AddSingleton(serviceAttribute.ServiceType, classType);
                                break;

                            case ServiceLifetime.Transient:
                                services.AddTransient(serviceAttribute.ServiceType, classType);
                                break;
                        }
                    }
                }
            }

            InitSK(services);

            return services;
        }

        /// <summary>
        /// 初始化SK
        /// </summary>
        /// <param name="services"></param>
        /// <param name="_kernel">可以提供自定义Kernel</param>
         static void InitSK(IServiceCollection services)
        {
            var handler = new OpenAIHttpClientHandler();
            services.AddTransient<Kernel>((serviceProvider) =>
            {
               
                var _kernel = Kernel.CreateBuilder()
                .AddOpenAIChatCompletion(
                    modelId: OpenAIOption.ChatModel,
                    apiKey: OpenAIOption.Key,
                    httpClient: new HttpClient(handler)
                        )
                .Build();

                //导入插件
                //if (!_kernel.Plugins.Any(p => p.Name == "test"))
                //{
                //    var pluginPatth = Path.Combine(RepoFiles.SamplePluginsPath(), "test");
                //    Console.WriteLine($"pluginPatth:{pluginPatth}");
                //    _kernel.ImportPluginFromPromptDirectory(pluginPatth);
                //}
                return _kernel;
            });
        }

    }
}
