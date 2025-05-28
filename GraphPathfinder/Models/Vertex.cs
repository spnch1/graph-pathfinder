namespace GraphPathfinder.Models
{
    public class Vertex
    {
        public int Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public override string ToString() => Id.ToString();
    }
}
