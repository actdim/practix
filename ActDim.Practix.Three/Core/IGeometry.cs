namespace THREE.Core
{
    public interface IGeometry : IElement
    {

    }

    public interface IGeometryContainer
    {
        IGeometry Geometry { get; set; }
    }
}
