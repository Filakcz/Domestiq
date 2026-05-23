namespace DomestiqAvalonia.Models;

public class RouteEdge
{
    public long TargetId { get; set; }
    public double Distance { get; set; }
    
    public RouteEdge(long targetId, double distance)
    {
        TargetId = targetId;
        Distance = distance;
    }
}
