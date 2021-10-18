using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace RazorPagesUI.PublicLibrary
{
    public class ConnectionString
    {
        public ConnectionString()
        {
        }

        public static string GetConnectionString(string connectionStringName = "Default")
        {
            string output = "";

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            var config = builder.Build();

            output = config.GetConnectionString(connectionStringName);


            return output;
        }
    }
}
