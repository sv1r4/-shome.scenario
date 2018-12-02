using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using shome.scene.provider.contract;

namespace shome.scene.processor
{
    class Program
    {
        static void Main(string[] args)
        {
            var ss = new ServiceCollection();
            ss.AddTransient<ISceneProvider, TestSceneProvider>();

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            var config = builder.Build();
            Console.WriteLine("Scene Processor");

        }
    }

    public class TestSceneProvider : ISceneProvider
    {

    }
}
