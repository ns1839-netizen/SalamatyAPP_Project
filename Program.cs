
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Salamaty.API.Middleware;
using Salamaty.API.Models;
using Salamaty.API.Services;
using SalamatyAPI.Data;

var builder = WebApplication.CreateBuilder(args);

// ===== 1. DbContext & SQL Server =====
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."),
        sqlServerOptionsAction: sqlOptions =>
        {
            // هذا هو السطر السحري الذي يحل مشكلة الـ transient failure
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        }
    ));

// ===== 2. Identity Configuration =====
// ابحثي عن جزء الـ Identity وعدليه ليكون هكذا
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedEmail = true; // إجبار تفعيل الإيميل
    options.Password.RequireDigit = true;
    // options.Password.LengthFormat = 8;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ===== 3. JWT Authentication =====
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = jwtSettings.GetValue<string>("Key") ?? "Default_Secure_Key_For_Salamaty_Project_2026";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.GetValue<string>("Issuer") ?? "SalamatyAPI",
        ValidAudience = jwtSettings.GetValue<string>("Audience") ?? "SalamatyUsers",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
    };
});

// ===== 4. Custom Application Services =====
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
// تسجيل خدمة الـ Home
builder.Services.AddScoped<IHomeService, HomeService>();
// ===== 5. Swagger with JWT Support =====
// ===== Swagger Configuration =====
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Salamaty.API", Version = "v1" });

    // تفعيل الـ Annotations والـ Filters لأنك قمتِ بتثبيتها
    c.EnableAnnotations();

    // 1. تعريف طريقة الحماية
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token."
    });

    // 2. ربط الحماية بالطلبات (هنا الحل)
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer" // لازم يطابق الاسم اللي فوق بالظبط
                }
            },
            Array.Empty<string>()
        }
    });
});

// ===== 6. ModelState Validation Customization =====
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value != null && e.Value.Errors.Count > 0)
            .ToDictionary(
                e => e.Key,
                e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray()
            );

        return new BadRequestObjectResult(new
        {
            success = false,
            message = "Validation failed",
            errors
        });
    };
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // هذا السطر يحول الـ Enum إلى نص (Male/Female) بدل أرقام (1/2)
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();

// ===== 7. CORS Policy =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", p =>
        p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// ===== 8. HTTP Request Pipeline (Middleware) =====

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Salamaty.API v1");
});


// Custom Exception Middleware
app.UseMiddleware<ExceptionMiddleware>();
// يسمح بالوصول للصور المرفوعة عبر الرابط
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();


