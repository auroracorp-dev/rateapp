using Dapper;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using System.Reflection.Emit;
using System.Text;
using WebAPI.Context;
using WebAPI.Manager;
using WebAPI.Manager.Interfaces;
using WebAPI.Models;

var builder = WebApplication.CreateBuilder(args);

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// PostgreSQL 연결
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

{
    // Invite 연결
    builder.Services.AddScoped<IBaseManager<Invite, Unit>, InviteManager>();
    builder.Services.AddScoped<IGetAll<InviteOutput, InviteFilter>, InviteManager>();
    builder.Services.AddScoped<IGet<InviteOutput>, InviteManager>();
    builder.Services.AddScoped<InviteManager>();

    // Review 연결
    builder.Services.AddScoped<IBaseManager<Review, ReviewInput>, ReviewManager>();
    builder.Services.AddScoped<IGetAll<ReviewOutput, ReviewFilter>, ReviewManager>();
    builder.Services.AddScoped<IGetID<ReviewOutput>, ReviewManager>();
    builder.Services.AddScoped<IUpdate<ReviewOutput, ReviewUpdateInput>, ReviewManager>();
    builder.Services.AddScoped<ReviewManager>();

    // Building 연결
    builder.Services.AddScoped<IBaseManager<Building, BuildingInput>, BuildingManager>();
    builder.Services.AddScoped<IGetAll<BuildingOutput, BuildingFilter>, BuildingManager>();
    builder.Services.AddScoped<IGetID<BuildingOutput>, BuildingManager>();
    builder.Services.AddScoped<IUpdate<BuildingOutput, BuildingUpdateInput>, BuildingManager>();
    builder.Services.AddScoped<BuildingManager>();

    // User 연결
    builder.Services.AddScoped<IBaseManager<User, UserInput>, UserManager>();
    builder.Services.AddScoped<IGetAll<UserOutput, UserFilter>, UserManager>();
    builder.Services.AddScoped<IGetID<UserOutput>, UserManager>();
    builder.Services.AddScoped<IUpdate<UserOutput, UserUpdateInput>, UserManager>();
    builder.Services.AddScoped<UserManager>();

    // Like 연결
    builder.Services.AddScoped<LikeManager>();

    // Auth 연결
    builder.Services.AddScoped<AuthManager>();
    builder.Services.AddScoped<CurrentUserService>();
    builder.Services.AddHttpContextAccessor();
}

// 컨트롤러 연결
builder.Services.AddControllers();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 인증
{
    builder.Services.AddAuthentication(options =>
    {
        // 인증 방법
        options.DefaultAuthenticateScheme = "Bearer";
        // 인증 실패시 리다이렉션 할때 사용할 인증방법
        options.DefaultChallengeScheme = "Bearer";
    })
    .AddJwtBearer("Bearer", options =>
    {
        var key = Convert.FromBase64String(builder.Configuration["Jwt:Key"]);

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,                             // 내 서버에서 발급한건지
            ValidateAudience = false,                           // 내 App에서 발급한건지
            ValidateIssuerSigningKey = true,                    // 위조 여부 확인
            IssuerSigningKey = new SymmetricSecurityKey(key),   // 서명 검증
            ValidateLifetime = true                             // 만료 여부 체크
        };
    });
    // Swagger 페이지 인증하는 버튼 생성
    builder.Services.AddSwaggerGen(c =>
    {
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] {}
            }
        });
    });
    builder.Services.AddHttpContextAccessor();
}
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies());
});
// builder.Services.AddAutoMapper(typeof(MappingProfile));

// ======================= 빌드 ======================= //


var app = builder.Build();

// 예외처리 담당
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var feature = context.Features.Get<IExceptionHandlerFeature>();
            var exception = feature?.Error;

            context.Response.ContentType = "application/json";

            context.Response.StatusCode = exception switch
            {
                UnauthorizedAccessException => 403,
                KeyNotFoundException => 404,
                InvalidOperationException => 409,
                _ => 500
            };

            await context.Response.WriteAsJsonAsync(new
            {
                message = exception?.Message
            });
        });
    });
}
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