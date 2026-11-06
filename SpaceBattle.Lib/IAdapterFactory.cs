using System.Collections.Generic;

namespace SpaceBattle.Lib;

public interface IAdapterFactory<T>
{
    T Create(IDictionary<string, object> obj);
}
