using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.DAL.Contract;
using Group2.SWP391.SportsBicycles.DAL.DB;
using Group2.SWP391.SportsBicycles.DAL.Implementation;
using Group2.SWP391.SportsBicycles.Services.Contract;
using Group2.SWP391.SportsBicycles.Services.Implementation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);  
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

var firebaseConfig = builder.Configuration.GetSection("Firebase").Get<Dictionary<string, object>>();
var privateKey = firebaseConfig["private_key"].ToString();

// 🔥 FIX newline
privateKey = privateKey.Replace("\\n", "\n");

firebaseConfig["private_key"] = privateKey;

FirebaseApp.Create(new AppOptions()
{
    Credential = GoogleCredential.FromJson(
        JsonSerializer.Serialize(firebaseConfig)
    )
});
// Add services to the container.
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddMemoryCache();

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<IBuyerListingService, BuyerListingService>();
builder.Services.AddScoped<IInspectorService, InspectorService>();
builder.Services.AddScoped<ISellerListingService, SellerListingService>();


builder.Services.AddHttpContextAccessor();


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSwaggerGen(c =>
{
c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
{
    Title = "SportsBicyclesStore.API",
    Version = "1.0.0"
});
c.DescribeAllParametersInCamelCase();
// Không cần nhập chữ Bearer
c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
{
    Name = "Authorization",
    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
    Scheme = "bearer",
    BearerFormat = "JWT",
    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
    Description = "Paste your JWT token here (no need to add 'Bearer')"
});
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
    c.OperationFilter<Group2.SWP391.SportsBicycles.API.Helpers.CookieParameterOperationFilter>();
});

builder.Services.AddScoped<IJwtService, JwtService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]!)
            ),

            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JWT:ValidIssuer"],

            ValidateAudience = true,
            ValidAudience = builder.Configuration["JWT:ValidAudience"],

            ValidateLifetime = true,
            RequireExpirationTime = true,

            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
