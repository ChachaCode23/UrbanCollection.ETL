namespace UrbanCollection.ETL.Extraccion;

public interface IExtractor<T>
{
    Task<List<T>> ExtraerAsync();
}