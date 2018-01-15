using AarekhLabs.Memcached.Api.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json.Serialization;
using Serilog;
using Swashbuckle.AspNetCore.Swagger;
using System.IO;

#pragma warning disable CS1591

namespace AarekhLabs.Memcached.Api
{
    public class Startup
    {
        private SwaggerDocConfig _swaggerConfig;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(Configuration).CreateLogger();
            Log.Logger.Information("[Startup] - Application startup configuration complete...");
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {   
            services.AddMvc(options =>
            {
                options.OutputFormatters.Add(new XmlSerializerOutputFormatter());
                options.RespectBrowserAcceptHeader = true;
            }).AddJsonOptions(options =>
            {
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            });

            services.AddCors();
            services.AddLogging(options => Configuration.GetSection("Logging").Bind(options));
            services.AddEnyimMemcached(options => Configuration.GetSection("enyimMemcached").Bind(options));

            // Initialize Swagger configuration
            services.Configure<SwaggerDocConfig>(Configuration.GetSection("SwaggerDoc"));
            var sc = new SwaggerDocConfig();
            Configuration.GetSection("SwaggerDoc").Bind(sc);
            _swaggerConfig = sc;

            if (sc != null && sc.EnableSwagger)
            {
                // Register the Swagger generator, defining one or more Swagger documents
                services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc(sc.Info.Version, new Info
                    {
                        Title = sc.Info.Title,
                        Version = sc.Info.Version,
                        Description = sc.Info.Description,
                        TermsOfService = sc.Info.Description,
                        Contact = new Contact
                        {
                            Name = sc.Info.Contact.Name,
                            Email = sc.Info.Contact.Email
                        }
                    });


                    //Set the comments path for the swagger json and ui.
                    var basePath = PlatformServices.Default.Application.ApplicationBasePath;
                    var xmlPath = Path.Combine(basePath, sc.IncludeXmlComments);
                    c.IncludeXmlComments(xmlPath);                    
                    c.DescribeAllEnumsAsStrings();
                    c.DescribeStringEnumsInCamelCase();
                });
            }

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory logger)
        {
            logger.AddConsole(Configuration.GetSection("Logging"));
            logger.AddSerilog();

            if (env.IsDevelopment())
            {
                logger.AddDebug();
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(builder =>
            {
                builder.AllowAnyHeader();
                builder.AllowAnyMethod();
                builder.AllowAnyOrigin();
            });

            app.UseMvc();
            app.UseEnyimMemcached();

            if (_swaggerConfig != null && _swaggerConfig.EnableSwagger)
            {
                // Enable middleware to serve generated Swagger as a JSON endpoint.
                app.UseSwagger();

                // Enable middleware to serve swagger-ui (HTML, JS, CSS etc.), specifying the Swagger JSON endpoint.
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AarekhLabs Memcached REST Api");
                });                
            }
        }
    }
}

#pragma warning restore CS1591
