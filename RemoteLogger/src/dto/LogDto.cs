namespace RemoteLogger.dto;

public class SecuredDto
{
    public required string Auth { get; set; }
}

public class LogDto : SecuredDto
{
    public long Timestamp { get; set; }

    public string? System { get; set; }
    
    public string? Module { get; set; }
    
    public sbyte? LogLevel { get; set; }
    
    public required string Message { get; set; }

    public override string ToString()
    {
        return $"{nameof(Timestamp)}: {Timestamp}, {nameof(System)}: {System}, {nameof(Module)}: {Module}, {nameof(LogLevel)}: {LogLevel}, {nameof(Message)}: {Message}";
    }
}

public class LogQueryDto : SecuredDto
{
    public string? System { get; set; }
    
    public string? Module { get; set; }
    
    public int? LogLevel { get; set; }
    
    public string? Message { get; set; }
    
    public long? From { get; set; }
    
    public long? To { get; set; }
    
    public int? Limit { get; set; }
    
    public int? Offset { get; set; }
    
    public bool? Asc { get; set; }
}

public class LogResponseDto
{
    public long Timestamp { get; set; }

    public required string System { get; set; }
    
    public required string Module { get; set; }
    
    public required string Message { get; set; }

    public override string ToString()
    {
        return $"{nameof(Timestamp)}: {Timestamp}, {nameof(System)}: {System}, {nameof(Module)}: {Module}, {nameof(Message)}: {Message}";
    }
}