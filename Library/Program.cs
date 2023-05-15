using Library.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Cors: Cross-Origin Resource Sharing ayarlarını yapılandırır.
builder.Services.AddCors(options =>
{
    options.AddPolicy("default_cors", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Add services to the container: Bağımlılıkları ve servisleri konteynere ekler.
builder.Services.AddControllers();

// Swagger: API dokümantasyonunu ve test arayüzünü sağlar.
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    options.OperationFilter<SecurityRequirementsOperationFilter>();
    string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
    //options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Library.xml"));
});

// Authentication and Authorization: Kimlik doğrulama ve yetkilendirme ayarlarını yapılandırır.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ClockSkew = TimeSpan.Zero,
            ValidateIssuer = false, // appsettings.json'da tutulması daha sağlıklı olacaktır
            ValidateAudience = false, // appsettings.json'da tutulması daha sağlıklı olacaktır
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetSection("AppSettings:Token").Value!))
        };
    });

builder.Services
    .AddIdentityCore<IdentityUser>()
    .AddEntityFrameworkStores<LibraryContext>()
    .AddDefaultTokenProviders();

// Database: Veritabanı bağlantısını yapılandırır.
builder.Services.AddDbContext<LibraryContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline: HTTP istekleri için gerekli ayarlamaları yapar.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use Cors: Cross-Origin Resource Sharing ayarlarını etkinleştirir.
app.UseCors("default_cors");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
