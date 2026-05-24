namespace DomestiqAvalonia.Models;

public class RouteEdge
{
    public long TargetId { get; set; }
    public double Distance { get; set; }
    
    public bool IsMotorway { get; set; }
    public bool IsOffroad { get; set; }
    
    public RouteEdge(long targetId, double distance, bool isMotorway, bool isOffroad)
    {
        TargetId = targetId;
        Distance = distance;
        IsMotorway = isMotorway;
        IsOffroad = isOffroad;
    }
}
