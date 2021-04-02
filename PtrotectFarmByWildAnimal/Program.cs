using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;

namespace ProtectFarmers
{
    class Program
    {
        public async static void Main(string [] args)
        {
            HostBuilder builder = new HostBuilder();
            await builder.RunConsoleAsync();
        }
    }
}

