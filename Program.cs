using ecommerce.DbContext;
using ecommerce.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MongoDB.Driver;
using ecommerce.Services.Interface;
using ecommerce.Services.Repository;
using ecommerce.Services;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Configure MongoDB settings from appsettings.json
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));

// Register MongoDbContext as a singleton
builder.Services.AddSingleton<MongoDbContext>();

// Add Identity with MongoDB
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(identityOptions =>
{
    // Identity password settings
    identityOptions.Password.RequireDigit = true;
    identityOptions.Password.RequiredLength = 6;
    identityOptions.Password.RequireNonAlphanumeric = false;
    identityOptions.Password.RequireUppercase = true;
    identityOptions.Password.RequireLowercase = true;

    // Lockout settings (optional)
    identityOptions.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    identityOptions.Lockout.MaxFailedAccessAttempts = 5;
    identityOptions.Lockout.AllowedForNewUsers = true;

    // User settings
    identityOptions.User.RequireUniqueEmail = true;
})
.AddMongoDbStores<ApplicationUser, ApplicationRole, Guid>(
    builder.Configuration["MongoDbSettings:ConnectionString"],
    builder.Configuration["MongoDbSettings:DatabaseName"])
.AddDefaultTokenProviders();


// Add Authentication using JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"], // Get from appsettings.json
        ValidAudience = jwtSettings["Audience"], // Get from appsettings.json
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"])) // Get from appsettings.json
    };
});

builder.Services.AddHttpClient<GeolocationService>();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<UserActivityLog>();
});

builder.Services.AddScoped<IUserGeolocationRepository, UserGeolocationRepository>();
builder.Services.AddScoped<IUserLogsRepository, UserLogsRepository>();
builder.Services.AddScoped<IUserGeolocationRepository, UserGeolocationRepository>();
builder.Services.AddScoped<IUserLogsRepository, UserLogsRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IShoppingCartRepository , ShoppingCartRepository>();




builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure middleware to handle forwarded headers
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
