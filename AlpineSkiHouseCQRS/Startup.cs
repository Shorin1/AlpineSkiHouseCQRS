using AlpineSkiHouseCQRS.Binders;
using AlpineSkiHouseCQRS.Commands;
using AlpineSkiHouseCQRS.Data.Implementations;
using AlpineSkiHouseCQRS.Data.Implementations.Repositories;
using AlpineSkiHouseCQRS.Data.Interfaces;
using AlpineSkiHouseCQRS.Data.Interfaces.Repositories;
using AlpineSkiHouseCQRS.Dispatchers;
using AlpineSkiHouseCQRS.Infrastructure;
using AlpineSkiHouseCQRS.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AlpineSkiHouseCQRS
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

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = true;
                    options.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateIssuer = true,
                        ValidIssuer = Configuration.GetValue<string>("JWT_Config:Issuer"),

                        ValidateAudience = true,
                        ValidAudience = Configuration.GetValue<string>("JWT_Config:Audience"),

                        ValidateLifetime = true,

                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.Default.GetBytes(Configuration.GetValue<string>("JWT_PASSWORD"))),
                        ValidateIssuerSigningKey = true
                    };
                }
                );

            services.AddControllersWithViews();
            services.RegisterServices(typeof(ICommandHandler<>));
            services.RegisterServices(typeof(IQueryHandler<>));
            services.RegisterServices(typeof(IRepository<>));
            services.AddTransient<IUnitOfWork, UnitOfWork>();
            services.AddTransient<IEnumerable<string>>((x) => new List<string>() { "new" });
            services.AddScoped<ApplicationDbContext>();
            services.AddSingleton<ICommandDispatcher, CommandDispatcher>();
            services.AddSingleton<IQueryDispatcher, QueryDispatcher>();
            services.AddSingleton<JSONModelBinder>();

            services.AddSingleton<IModelBinderDispatcher, ModelBinderDispatcher>((provider) =>
                new ModelBinderDispatcher(
                    provider.GetService<JSONModelBinder>(),
                    provider.GetService<JSONModelBinder>(), 
                    provider));

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            //������ �������� ����������
            services.RegisterCommandHandlers();
            services.RegisterQueryHandlers();

            string connectionString = Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<IApplicationDbContext, ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));

            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            app.UseRouting();
            //app.UseMiddleware<CQRSRouting>();

           // app.UseMiddleware<JwtAuthorization>();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
            });

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    spa.UseReactDevelopmentServer(npmScript: "start");
                }
            });

            InitDb(app).Wait();
        }

        public async Task InitDb(IApplicationBuilder app)
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                
                var context = scope.ServiceProvider.GetService<ApplicationDbContext>();

                if((await context.Users.FirstOrDefaultAsync()) == null)
                {
                    var registrationHandler = scope.ServiceProvider.GetService<ICommandHandler<RegistrationCommand>>();
                    var command = new RegistrationCommand()
                    {
                        Email = "some@mail.ru",
                        BirthDate = DateTime.Parse("10.09.1998"),
                        FirstName = "Petr",
                        MiddleName = "Sergeevich",
                        SecondName = "Sidorov",
                        Password = "12345678"
                    };
                    await registrationHandler.Handle(command);
                }
            }
        }
    }
}
