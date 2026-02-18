using RemoteLogger;
using RemoteLogger.dto;

var builder = WebApplication.CreateBuilder(args);

// docker build -f Dockerfile -t remote-logger:v1 ..

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var authHeader = app.Configuration.GetSection("AuthHeader").Value;

if (authHeader == null)
{
    app.Logger.LogWarning("Running app with no authorization header. All requests will be permitted");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// todo set this
// app.UseAuthorization();

// todo initialize. Check DB connection and table. Set state


// todo to config
var connectionStr = "Server=127.0.0.1;Port=3307;Database=test;User=root;Password=my-secret-pw;";
var dbWrapper = new DbWrapper(connectionStr, "RemoteLog");


app.MapGet("/status", () => "Ok");


app.MapPost("/log", IResult (LogDto logDto) =>
{
    // store posts on DB
    app.Logger.LogTrace("POST /log received log");
    if (!CheckAuthorization(logDto))
    {
        return Results.Unauthorized();
    }
    logDto = ValidateLogDto(logDto);
    app.Logger.LogDebug("POST /log validated log {LogDto}", logDto);
    var res = dbWrapper.StoreLog(logDto);
    app.Logger.LogDebug("POST /log stored log. Result: {result}", res);

    return TypedResults.Ok(res);
});

app.MapPost("/query-logs", IResult (LogQueryDto logQueryDto) =>
{
    app.Logger.LogTrace("POST /query-logs received log query");
    if (!CheckAuthorization(logQueryDto))
    {
        return Results.Unauthorized();
    }
    logQueryDto = ValidateLogQueryDto(logQueryDto);
    app.Logger.LogDebug("POST /query-logs validated log query {LogQueryDto}", logQueryDto);
    var logs = dbWrapper.GetLogs(logQueryDto);
    app.Logger.LogDebug("POST /query-logs successfully retrieved logs. Count: {LogsLength}", logs.Length);
    
    return TypedResults.Ok(logs);
});


bool CheckAuthorization(SecuredDto dto)
{
    return authHeader == null || 
           dto.Auth.Equals(authHeader) || 
           app.Environment.IsDevelopment() && dto.Auth.Equals("0");
}


LogDto ValidateLogDto(LogDto logDto)
{
    logDto.System ??= string.Empty;
    logDto.Module ??= string.Empty;
    logDto.LogLevel ??= -1;
    return logDto;
}

LogQueryDto ValidateLogQueryDto(LogQueryDto logQueryDto)
{
    // todo from config
    if (logQueryDto.Limit == null || logQueryDto.Limit < 0)
        logQueryDto.Limit ??= 100;
    
    if (logQueryDto.Offset == null || logQueryDto.Offset < 0)
        logQueryDto.Offset ??= 0;
    
    logQueryDto.Asc ??= true;
    return logQueryDto;
}

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}