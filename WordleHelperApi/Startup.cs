using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WordleHelperApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;

            var globalbuilder = new ConfigurationBuilder()
                  .SetBasePath(env.ContentRootPath)
                         .AddJsonFile("globalconfig.json");
            var globalConfiguration = globalbuilder.Build();

            string stagingEnvironment = globalConfiguration["StagingEnvironment"];

            var builder = new ConfigurationBuilder()
                 .SetBasePath(env.ContentRootPath)
               .AddJsonFile("config.json")
               .AddJsonFile($"config.{stagingEnvironment}.json", optional: true);
            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        const string MyAllowSpecificOrigins = "_myAllowSpecificOrigins"; // for CORS
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(name: MyAllowSpecificOrigins,
                                  policy =>
                                  {
                                      policy.WithOrigins("http://localhost:8100",
                                                          "http://wordle.swottle.com")
                                                              .AllowAnyHeader()
                                                              .AllowAnyMethod();
                                  });
            }); // for CORS

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "WordleHelperApi", Version = "v1" });
            });

            services.Configure<ApplicationConfiguration>(
         Configuration.GetSection("ApplicationConfiguration"));
           // services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {


            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WordleHelperApi v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors(MyAllowSpecificOrigins); // for CORS

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
