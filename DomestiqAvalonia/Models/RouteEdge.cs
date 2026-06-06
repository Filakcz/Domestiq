namespace DomestiqAvalonia.Models;

public class RouteEdge
{
    public long TargetId { get; set; }
    public double Distance { get; set; }
    
    public bool IsMotorway { get; set; }
    public bool IsOffroad { get; set; }
    public int Priority { get; set; } // 1-5
    
    public RouteEdge(long targetId, double distance, bool isMotorway, bool isOffroad, int priority = 4)
    {
        TargetId = targetId;
        Distance = distance;
        IsMotorway = isMotorway;
        IsOffroad = isOffroad;
        Priority = priority;
    }
}
