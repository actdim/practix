namespace ActDim.Practix.Abstractions.DataAccess.Generic
{
    public interface IDbEntityFactory<out TEntity>
    {
        TEntity CreateInstance();
    }
}