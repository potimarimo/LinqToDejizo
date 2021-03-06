﻿using Marimo.ExpressionParserCombinator;
using Marimo.LinqToAmazonProductAdvertisingApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Marimo.ExpressionParserCombinator.ParserCreaters;

namespace Marimo.LinqToDejizo
{
    public class PaaProvider : QueryProvider
    {
        string AwsAccessKeyID { get; }
        string AwsSecretKey { get; }
        string AssociateTag { get; }
        string EndPoint { get; }
        HMAC Signer { get; }

        public PaaProvider(string awsAccessKeyID, string awsSecretKey, string associateTag)
        {
            AwsAccessKeyID = awsAccessKeyID;
            AwsSecretKey = awsSecretKey;
            AssociateTag = associateTag;
            EndPoint = "ecs.amazonaws.jp";
            Signer = new HMACSHA256(Encoding.UTF8.GetBytes(awsSecretKey));
        }

        public override object Execute(Expression expression)
        {
            
            return Task.Run(async () =>
            {
                var parameters = new Dictionary<string, String>()
                {
                    ["Service"] = "AWSECommerceService",
                    ["Operation"] = "ItemSearch",
                    ["AWSAccessKeyId"] = AwsAccessKeyID,
                    ["AssociateTag"] = AssociateTag,
                    ["SearchIndex"] = "Books",
                    ["ResponseGroup"] = "Images,ItemAttributes",
                    ["Keywords"] = ParseLinqRoot(expression)
                };
                return await FetchTitleAsync(Sign(parameters));
            }).GetAwaiter().GetResult();
        }
        public string Sign(IDictionary<string, string> request) =>
            $@"http://{
                EndPoint}{
                "/onca/xml"}?{
                GetQueryString(request)
                }&Signature={
                WebUtility.UrlEncode(
                    Convert.ToBase64String(
                        Signer.ComputeHash(
                            Encoding.UTF8.GetBytes(
                                string.Join("\n",
                                new[]{
                                    "GET",
                                    EndPoint,
                                    "/onca/xml",
                                    GetQueryString(request)
                                })))))}";

        string GetQueryString(IDictionary<string, string> request) =>
            ConstructCanonicalQueryString(
                new SortedDictionary<string, string>(request, new ParamComparer())
                {
                    ["AWSAccessKeyId"] = AwsAccessKeyID,
                    ["Timestamp"] = GetTimestamp()
                });

        string GetTimestamp() => DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        string ConstructCanonicalQueryString(SortedDictionary<string, string> sortedParams) =>
            string.Join("&", sortedParams.Select(x => $"{WebUtility.UrlEncode(x.Key)}={WebUtility.UrlEncode(x.Value)}"));

        static HttpClient client = new HttpClient();

        private async Task<string> FetchTitleAsync(string url)
        {
            var str = await (await client.GetAsync(url)).Content.ReadAsStringAsync();

            var xml = XDocument.Parse(str);

            var ns = xml.Root.Name.Namespace;
            var errorMessageNodes = xml.Descendants(ns + "Message").ToList();
            if (errorMessageNodes.Any())
            {
                var message = errorMessageNodes[0].Value;
                return "Error: " + message;
            }

            var title = xml.Descendants(ns + "Title").First();
            return title.Value;
        }

        public override string GetQueryText(Expression expression)
        {
            try
            {
                return "query text";
                return expression?.ToString() ?? "null";
            }
            catch
            {
                return "query text";
            }
        }
        class ParamComparer : IComparer<string>
        {
            public int Compare(string p1, string p2)
            {
                return string.CompareOrdinal(p1, p2);
            }
        }

        private string ParseLinqRoot(Expression expression)
        {
            var selectLambda = Lambda();

            var constWord = Constant();

            var valiableWord = Member();

            var word = constWord | valiableWord;
                

            var singleCondition = _
                ((Book b, string p) => b.IncludeKeyword(p),
                    arguments: new[] { word });

            var whereFunc =
                Unary(
                    operand: Lambda(singleCondition));

            var query =
                _((IQueryable<object> c) => c.Where(x => true),
                    arguments: new[] { null, whereFunc })
                |
                _((IQueryable<object> c) => c.Select(x => x),
                    arguments: new ExpressionParser[]
                    {
                        MethodCall(
                            arguments:new[]{ null, whereFunc }),
                        Unary(
                            operand: selectLambda)
                    });

            var count =
                _((IQueryable<object> c) => c.Count(),
                    arguments: new[] { query });

            var first =
                _((IQueryable<object> c) => c.First(),
                    arguments: new[] { query });

            var firstOrDefault =
                _((IQueryable<object> c) => c.FirstOrDefault(),
                    arguments: new[] { query });

            var single =
                _((IQueryable<object> c) => c.Single(),
                    arguments: new[] { query });

            var singleOrDefault =
                _((IQueryable<object> c) => c.SingleOrDefault(),
                    arguments: new[] { query });

            var lastMethod = count | first | single | firstOrDefault | singleOrDefault;

            var wholeExtention = lastMethod | query;

            string keyword = null;
            singleCondition.Action = m => keyword = (string)((ConstantExpression)m.Arguments[0]).Value;

            wholeExtention.Parse(expression);

            return keyword;
        }
    }
}
