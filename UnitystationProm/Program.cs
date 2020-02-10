using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using Prometheus;
using System.Text.Json;

namespace UnitystationProm
{
    class Program
    {
        private static readonly Gauge Players =
            Metrics.CreateGauge("unitystation_players", "Amount of players on server", new GaugeConfiguration{
                LabelNames = new [] {"server"}
            });

        static async Task Main()
        {
            var server = new MetricServer(hostname: "*", port: 7776);
            Metrics.SuppressDefaultMetrics();
            var http = new HttpClient();
            server.Start();

            Metrics.DefaultRegistry.AddBeforeCollectCallback(async (cancel) =>
            {
                Console.WriteLine("Scraped");
                var res = await http.GetStringAsync("https://api.unitystation.org/serverlist");
                var par = JsonSerializer.Deserialize<RootObject>(res);
                var names = par.servers.Select(s => s.ServerName);
                var oldNames = Players.GetAllLabelValues().Select(v => v.FirstOrDefault());

                foreach(var old in oldNames.Except(names)){
                    Players.RemoveLabelled(old);
                }

                foreach(var server in par.servers){
                    Players.WithLabels(server.ServerName).Set(server.PlayerCount);
                }
            });

            Console.WriteLine("Started");
            while (true)
            {
                await Task.Delay(5000);
            }
        }
    }
}
