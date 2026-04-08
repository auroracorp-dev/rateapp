using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Reflection.Emit;
using WebAPI.Context;
using WebAPI.Manager;
using WebAPI.Manager.Interfaces;
using WebAPI.Models;

var builder = WebApplication.CreateBuilder(args);

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

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
    builder.Services.AddScoped<ReviewManager>();

    // Building 연결
    builder.Services.AddScoped<IBaseManager<Building, BuildingInput>, BuildingManager>();
    builder.Services.AddScoped<IGetAll<BuildingOutput, BuildingFilter>, BuildingManager>();
    builder.Services.AddScoped<IGetID<BuildingOutput>, BuildingManager>();
    builder.Services.AddScoped<BuildingManager>();

    // User 연결
    builder.Services.AddScoped<IBaseManager<User, UserInput>, UserManager>();
    builder.Services.AddScoped<IGetAll<UserOutput, UserFilter>, UserManager>();
    builder.Services.AddScoped<IGetID<UserOutput>, UserManager>();
    builder.Services.AddScoped<UserManager>();

    // Like 연결
    builder.Services.AddScoped<LikeManager>();

    // Auth 연결
    builder.Services.AddScoped<AuthManager>();
}

// 컨트롤러 연결
builder.Services.AddControllers();
builder.Services.AddControllers()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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