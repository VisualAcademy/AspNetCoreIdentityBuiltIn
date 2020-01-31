using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VisualAcademy.Admin.Areas.Identity;
using VisualAcademy.Admin.Data;
using Microsoft.AspNetCore.Identity.UI.Services;
using VisualAcademy.Admin.Services;

namespace VisualAcademy.Admin
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));

            //services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<ApplicationDbContext>();
            services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();
            services.AddSingleton<WeatherForecastService>();

            services.AddTransient<IEmailSender, EmailSender>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });

            CreateBuiltInUsersAndRoles(serviceProvider).Wait();
        }

        private async Task CreateBuiltInUsersAndRoles(IServiceProvider serviceProvider)
        {
            //[0] DbContext ��ü ����
            var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Database.EnsureCreated(); // �����ͺ��̽��� �����Ǿ� �ִ��� Ȯ�� 

            // �⺻ ���� ����� �� ������ �ϳ��� ������(��, ó�� �����ͺ��̽� �����̶��)
            if (!dbContext.Users.Any() && !dbContext.Roles.Any())
            {
                string domainName = "a.com";

                //[1] Groups(Roles)
                var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                //[1][1] ('Administrators', '������ �׷�', 'Group', '���� ���α׷��� �� �����ϴ� ���� �׷� ����')
                //[1][2] ('Everyone', '��ü ����� �׷�', 'Group', '���� ���α׷��� ����ϴ� ��� ����� �׷� ����')
                //[1][3] ('Users', '�Ϲ� ����� �׷�', 'Group', '�Ϲ� ����� �׷� ����')
                //[1][4] ('Guests', '������ �׷�', 'Group', '�Խ�Ʈ ����� �׷� ����')
                string[] roleNames = { "Administrators", "Everyone", "Users", "Guests" };
                foreach (var roleName in roleNames)
                {
                    var roleExist = await roleManager.RoleExistsAsync(roleName);
                    if (!roleExist)
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName)); // ��Ʈ�� �׷� ����
                    }
                }

                //[2] Users
                var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
                //[2][1] Administrator
                // ('Administrator', '������', 'User', '���� ���α׷��� �� �����ϴ� ����� ����')
                IdentityUser administrator = await userManager.FindByEmailAsync($"administrator@{domainName}");
                if (administrator == null)
                {
                    administrator = new IdentityUser()
                    {
                        UserName = $"administrator@{domainName}",
                        Email = $"administrator@{domainName}",
                        EmailConfirmed = true,
                    };
                    await userManager.CreateAsync(administrator, "Pa$$w0rd");
                }

                //[2][2] Guest
                // ('Guest', '�Խ�Ʈ �����', 'User', '�Խ�Ʈ ����� ����')
                IdentityUser guest = await userManager.FindByEmailAsync($"guest@{domainName}");
                if (guest == null)
                {
                    guest = new IdentityUser()
                    {
                        UserName = "Guest",
                        Email = $"guest@{domainName}",
                    };
                    await userManager.CreateAsync(guest, "Pa$$w0rd");
                }


                //[2][3] Anonymous
                // ('Anonymous', '�͸� �����', 'User', '�͸� ����� ����')
                IdentityUser anonymous = await userManager.FindByEmailAsync($"anonymous@{domainName}");
                if (anonymous == null)
                {
                    anonymous = new IdentityUser()
                    {
                        UserName = "Anonymous",
                        Email = $"anonymous@{domainName}",
                    };
                    await userManager.CreateAsync(anonymous, "Pa$$w0rd");
                }

                //[3] UsersInRoles: AspNetUserRoles Table
                await userManager.AddToRoleAsync(administrator, "Administrators");
                await userManager.AddToRoleAsync(administrator, "Users");
                await userManager.AddToRoleAsync(guest, "Guests");
                await userManager.AddToRoleAsync(anonymous, "Guests");
            }
        }
    }
}
