using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Salamaty.API.Middleware;
using Salamaty.API.Models.ProfileModels;
using Salamaty.API.Services;
using Salamaty.API.Services.AuthServices;
using Salamaty.API.Services.HomeServices;
using Salamaty.API.Services.PrescriptionServices;
using SalamatyAPI.Data;

var builder = WebApplication.CreateBuilder(args);

// ===== 1. DbContext & SQL Server =====
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."),
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        }
    ));

// ===== 2. Identity Configuration =====
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedEmail = true;
    options.Password.RequireDigit = true;
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
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IHomeService, HomeService>();
builder.Services.AddScoped<IPrescriptionService, PrescriptionService>();
builder.Services.AddHttpContextAccessor();

// ===== 5. ModelState Validation Customization =====
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

// ===== 6. Swagger Configuration =====
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Salamaty.API", Version = "v1" });
    c.EnableAnnotations();
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();

// ===== 7. CORS Policy =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", p =>
        p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});
// إضافة دعم الـ HttpClient للسيرفيس بتاعتك
builder.Services.AddHttpClient<IPrescriptionService, PrescriptionService>();
builder.Services.AddHttpClient<IPrescriptionService, PrescriptionService>(client =>
{
    // هنخلي السيرفر بتاعنا يصبر لمدة 3 دقائق (180 ثانية) على الـ AI
    client.Timeout = TimeSpan.FromMinutes(5);
});
var app = builder.Build();

// ============================================================
// ===== 8. Middleware Pipeline (تم تعديل الترتيب هنا) =====
// ============================================================

// أ. أولاً: تفعيل الملفات الثابتة (wwwroot)
app.UseStaticFiles();

// ب. ثانياً: إعداد المجلدات المخصصة قبل أي Routing أو Exceptions
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "Uploads");
if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/Uploads"
});

var medicinePath = Path.Combine(app.Environment.ContentRootPath, "medicine_images");
if (!Directory.Exists(medicinePath)) Directory.CreateDirectory(medicinePath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(medicinePath),
    RequestPath = "/medicine_images"
});

// ج. ثالثاً: Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Salamaty.API v1");
});

// د. رابعاً: معالجة الأخطاء (بعد التأكد أن الطلب ليس صورة)
app.UseMiddleware<ExceptionMiddleware>();

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ===== 9. Database Seeding =====
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var environment = services.GetRequiredService<IWebHostEnvironment>();
        DbSeeder.Seed(context, environment);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during database seeding.");
    }
}

app.Run();