using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Network.Adapters;

/// <summary>
/// Um pool genérico de objetos reutilizáveis para reduzir a pressão no Garbage Collector
/// Especialmente útil para operações de alta frequência em jogos MMO
/// </summary>
/// <typeparam name="T">O tipo de objeto a ser armazenado no pool</typeparam>
public class ObjectPool<T> where T : class
{
    private readonly ConcurrentBag<T> _objects;
    private readonly Func<T> _objectGenerator;
    private readonly Action<T>? _resetAction;
    private readonly int _maxSize;

    /// <summary>
    /// Inicializa uma nova instância da classe ObjectPool
    /// </summary>
    /// <param name="objectGenerator">Função que cria novos objetos quando o pool está vazio</param>
    /// <param name="resetAction">Ação opcional para resetar o estado de um objeto antes de retorná-lo ao pool</param>
    /// <param name="initialSize">Número inicial de objetos no pool</param>
    /// <param name="maxSize">Tamanho máximo do pool. Quando atingido, objetos liberados são descartados</param>
    public ObjectPool(Func<T> objectGenerator, Action<T>? resetAction = null, int initialSize = 0, int maxSize = 100)
    {
        _objects = new ConcurrentBag<T>();
        _objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
        _resetAction = resetAction;
        _maxSize = maxSize;

        // Pré-alocação de objetos
        for (int i = 0; i < initialSize; i++)
        {
            _objects.Add(_objectGenerator());
        }
    }

    /// <summary>
    /// Obtém um objeto do pool ou cria um novo se o pool estiver vazio
    /// </summary>
    /// <returns>Um objeto do tipo T</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get() => _objects.TryTake(out T? item) ? item : _objectGenerator();

    /// <summary>
    /// Retorna um objeto ao pool para reutilização
    /// </summary>
    /// <param name="item">O objeto a ser retornado ao pool</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Return(T item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        
        // Reseta o objeto se necessário
        _resetAction?.Invoke(item);
        
        // Adiciona o objeto de volta ao pool apenas se não exceder o tamanho máximo
        if (_objects.Count < _maxSize)
        {
            _objects.Add(item);
        }
        // Caso contrário, o objeto será coletado pelo GC naturalmente
    }
}