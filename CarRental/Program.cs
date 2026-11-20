using System.Security.Cryptography;
using System.Text;
using CarRentalServices.Contracts;
using CarRentalServices.Repositories;
using CarRentalServices.Services;
using CarRental.Data;
using CarRental.Models;
using CarRental.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection before starting the API.");
}

var databaseEngine = builder.Configuration["Database:Engine"] ?? "MySql";
var databaseVersion = Version.Parse(builder.Configuration["Database:Version"] ?? "8.0.0");
ServerVersion serverVersion = databaseEngine.Equals("MariaDb", StringComparison.OrdinalIgnoreCase)
    ? new MariaDbServerVersion(databaseVersion)
    : new MySqlServerVersion(databaseVersion);
builder.Services.AddDbContext<AppDbContext>(options => options.UseMySql(connectionString, serverVersion));

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) && builder.Environment.IsDevelopment())
{
    jwtKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    builder.Configuration["Jwt:Key"] = jwtKey;
    builder.Logging.AddConsole();
    Console.WriteLine("A temporary JWT key was generated for this development session.");
}

if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException("Configure Jwt:Key with at least 32 characters using a secret store.");
}

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer) &&
                         !string.IsNullOrWhiteSpace(options.Audience) &&
                         options.ExpireMinutes is > 0 and <= 1440,
        "JWT issuer, audience, and expiration must be configured.")
    .ValidateOnStart();

builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            NameClaimType = System.Security.Claims.ClaimTypes.Name,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

// Add services to the container.
builder.Services.AddScoped<IDataService, DataService>();
builder.Services.AddScoped<IMySqlDataService, MySqlDataService>();
builder.Services.AddKeyedScoped<ICategoryRepository, MySqlCategoryRepository>("MySql");
builder.Services.AddKeyedScoped<ICategoryRepository, CategoryRepository>("MsSql");
builder.Services.AddKeyedScoped<ICarsRepository, CarRepository>("MsSql");
builder.Services.AddKeyedScoped<ICarsRepository, CarsRepository>("MySql");
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ICountryRepository, CountryRepository>();
builder.Services.AddScoped<ILocationRepository, LocationRepository>();
builder.Services.AddKeyedScoped<IColorRepository, ColorRepository>("MySql");
builder.Services.AddKeyedScoped<IBrandRepository, BrandRepository>("MySql");

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa el token JWT obtenido en /api/auth/login."
    });
    options.OperationFilter<AuthorizeCheckOperationFilter>();
});


var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
