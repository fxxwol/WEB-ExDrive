using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;

using JWTAuthentication.Authentication;

using WebPWrecover.Services;

using exdrive_web.Models;
using exdrive_web.Configuration;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("ApplicationDbContextConnection"); 
builder.Services.AddDbContext<ApplicationDbContext>(options =>
     options.UseSqlServer(connectionString)); builder.Services.AddDefaultIdentity<ApplicationUser>
    (options => options.SignIn.RequireConfirmedAccount = true)
      .AddEntityFrameworkStores<ApplicationDbContext>();

var connectionStrings = ConnectionStrings.GetInstance(
    connectionString, 
    builder.Configuration.GetConnectionString("StorageConnection"),
    builder.Configuration.GetConnectionString("VirusTotalToken"),
    builder.Configuration.GetConnectionString("SendGridKey"));

builder.Services.Configure<CookieTempDataProviderOptions>(options => {
    options.Cookie.IsEssential = true;
});

builder.Services.AddTransient<IEmailSender, EmailSender>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

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

var botfiles = new DeleteTemporaryTimer(7, "botfiles");
botfiles.SetTimer();
var trashcan = new DeleteTemporaryTimer(15, "trashcan");
trashcan.SetTimer();

app.Run();