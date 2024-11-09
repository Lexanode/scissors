namespace Core.RepositoryBase.Model;

/// <summary>
/// Базовая модель репозитория
/// </summary>
public abstract class DalModelBase<T>
{
    /// <summary>
    /// Id
    /// </summary>
    public T Id { get; set; }
}