namespace DomestiqAvalonia.Models;

public class RouteEdge
{
    public RouteNode Source { get; set; }
    public RouteNode Target { get; set; }
    public double Distance { get; set; }
    
    public RouteEdge(RouteNode source, RouteNode target, double distance)
    {
        Source = source;
        Target = target;
        Distance = distance;
    }
}
