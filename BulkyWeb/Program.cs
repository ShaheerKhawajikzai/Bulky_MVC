using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Bulky.Utitlity;
using Stripe;
using Microsoft.CodeAnalysis.Options;
using Bulky.DataAccess.DBInitializer;
using Bulky.Models.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//for stripe
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));

//for session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(option =>
{
    option.IdleTimeout = TimeSpan.FromMinutes(100);
    option.Cookie.HttpOnly = true;
    option.Cookie.IsEssential = true;
});

//for facebook login
object value = builder.Services.AddAuthentication().AddFacebook(options =>
{
    options.AppId = "761379202032076";
    options.AppSecret = "81c64e76f3c5a38ff0e26b5f5432f23f";
});

//for microsoft login
builder.Services.AddAuthentication().AddMicrosoftAccount(options =>
{
    options.ClientId = "045a2393-ab2b-4d29-8abe-84a2bc8ada21";
    options.ClientSecret = "4hy8Q~uueoblmkG5gnfVS_AttffcvffsKaaCZctk";
});
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddDefaultUI()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(option =>
{
    option.LoginPath = $"/Identity/Account/Login";
    option.LogoutPath = $"/Identity/Account/Logout";
    option.AccessDeniedPath = $"/Identity/Account/AccessDenied";

});

builder.Services.AddRazorPages();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IApplicationUserRepository, ApplicationUserRepository>();
builder.Services.AddScoped<IShoppingCartRepository, ShoppingCartRepository>();
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<IOrderDetailRepository, OrderDetailRepository>();
builder.Services.AddScoped<IOrderHeaderRepository, OrderHeaderRepository>();
builder.Services.AddScoped<IDBInitializer, DBInitializer>();
builder.Services.AddScoped<IProductImageRepository, ProductImageRepository>();


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
//for stripe
StripeConfiguration.ApiKey = builder.Configuration.GetSection("Stripe:SecretKey").Get<String>();
app.UseRouting();
app.MapRazorPages();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();
SeedDatabase();
app.MapControllerRoute(
    name: "default",
    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");

app.Run();

void SeedDatabase()
{
    using (var scope = app.Services.CreateScope())
    {
        var dbInitializer = scope.ServiceProvider.GetRequiredService<IDBInitializer>();
        dbInitializer.Initialize();
    }
}