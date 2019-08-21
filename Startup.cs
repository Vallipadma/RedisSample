using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;

namespace app
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var myCredHubSecrets = JObject.Parse(Environment.GetEnvironmentVariable("VCAP_SERVICES"));
            Console.WriteLine("retrieved vcap service");

            string host = myCredHubSecrets["p-redis"][0]["credentials"]["host"].ToString();
            Console.WriteLine("host:" + host);

            string port = myCredHubSecrets["p-redis"][0]["credentials"]["port"].ToString();
            Console.WriteLine("port:" + port);

            string password = myCredHubSecrets["p-redis"][0]["credentials"]["password"].ToString();
            Console.WriteLine("password:" + password);

            string strDBConStr = host + ":" + port + ",password=" + password;

            Console.WriteLine("connectionstrin" + strDBConStr);

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            var redis = ConnectionMultiplexer.Connect(strDBConStr);
            
            services.AddDataProtection()
                    .PersistKeysToStackExchangeRedis(redis, "DataProtectionKeys");

            services.AddStackExchangeRedisCache(option =>
            {
                option.Configuration = strDBConStr;
                option.InstanceName = "RedisInstance";
            });            

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.Name = "AppTest";
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            
            app.UseSession();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
