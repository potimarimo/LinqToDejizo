using AmazonProductAdvtApi;
using Marimo.LinqToAmazonProductAdvertisingApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Xml.Linq;
using Xunit;

namespace LinqToAmazonProductAdvertisingApiTest
{



    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            var config =
                new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName)
                .AddJsonFile(@"appsettings.json")
                .AddJsonFile(@"secretsettings.json", true)
                .Build();

            var source = new PaaSource(
                    config["awsAccessKeyID"],
                    config["awsSecretKey"],
                    config["awsAccessKeyId"]
                );

            var query =
                from book in source.Books
                where book.Include("\"初めてのRuby\"")
                select book.Title;

            query.First().Is("初めてのRuby");
        }
    }
}
