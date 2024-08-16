using LOGIN.Services.Interfaces;
using LOGIN.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using LOGIN.Services;
using LOGIN.LogsCouchDBServices;

namespace LOGIN
{
    public class Startup
    {
        private readonly string _corsPolicy = "CorsPolicy";

        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            var connString = Configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseMySql(connString, ServerVersion.AutoDetect(connString),
                    mySqlOptions => mySqlOptions.SchemaBehavior(MySqlSchemaBehavior.Ignore)));

            // Add custom services
            services.AddTransient<IUserService, UserService>();
            services.AddTransient<IEmailService, EmailService>();
            services.AddTransient<IComunicateServices, ComunicateServices>();
            services.AddTransient<IAPiSubscriberServices, APiSubscriberServices>();
            services.AddTransient<IReportService, ReportService>();
            services.AddTransient<IBlocksService, BlocksService>();
            services.AddScoped<IDistrictsPointsService, DistrictsPointsService>();
            services.AddTransient<ILinesService, LinesService>();
            services.AddTransient<INeighborhoodsColoniesService, NeighborhoodsColoniesService>();
            services.AddTransient<IRegistrationWaterService, RegistrationWaterService>();
            services.AddTransient<IRegistrationWaterNeighborhoodsColoniesService, RegistrationWaterNeighborhoodsColoniesService>();
            services.AddTransient<IStateService, StateService>();

            // Add AutoMapper service
            services.AddAutoMapper(typeof(Startup));

            //Add logs services
            services.AddHttpContextAccessor();
            services.AddSingleton<CouchDBLogger>();

            services.AddTransient<SomeService>();

            // Add Identity
            services.AddIdentity<UserEntity, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                // Configure password settings here
            }).AddEntityFrameworkStores<ApplicationDbContext>()
              .AddDefaultTokenProviders();

            // Add Authentication
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = Configuration["Jwt:ValidAudience"],
                    ValidIssuer = Configuration["Jwt:ValidIssuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(Configuration["Jwt:Secret"]))
                };
            });

            // Configure CORS
            services.AddCors(options =>
            {
                options.AddPolicy(_corsPolicy, builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyHeader()
                           .AllowAnyMethod()
                           .AllowCredentials()
                           .WithExposedHeaders("Content-Disposition");
                });
            });

            //Add Services 

            services.AddHttpClient<APiSubscriberServices>();

            services.AddControllers();

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                app.UseExceptionHandler("/error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseMiddleware<LoggingMiddleware>();

            // Use CORS
            app.UseCors(_corsPolicy);

            // Use Authentication
            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // Ejecutar el log de prueba en segundo plano
            //Task.Run(async () =>
            //{
            //    try
            //    {
            //        var someService = app.ApplicationServices.GetService<SomeService>();
            //        await someService.RunTestLog();
            //    }
            //    catch (Exception ex)
            //    {
            //        var logger = app.ApplicationServices.GetService<ILogger<Startup>>();
            //        logger.LogError(ex, "Error al ejecutar el log de prueba.");
            //    }
            //});
        }
    }
}
