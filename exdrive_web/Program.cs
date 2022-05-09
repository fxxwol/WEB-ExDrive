using Microsoft.EntityFrameworkCore;
//using exdrive_web.Areas.Identity.Data;
using JWTAuthentication.Authentication;
using Microsoft.AspNetCore.Http.Features;
using exdrive_web.Models;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("ApplicationDbContextConnection"); builder.Services.AddDbContext<ApplicationDbContext>(options =>
     options.UseSqlServer(connectionString)); builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
      .AddEntityFrameworkStores<ApplicationDbContext>();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

DeleteTemporaryTimer botfiles = new DeleteTemporaryTimer(7, "botfiles");
botfiles.SetTimer();
DeleteTemporaryTimer trashcan = new DeleteTemporaryTimer(15, "trashcan");
trashcan.SetTimer();
app.Run();