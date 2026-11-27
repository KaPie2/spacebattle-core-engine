using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SpaceBattle.Lib;


public class ExceptionHandlerRouter
{
    private readonly ConcurrentDictionary<(Type CommandType, Type ExceptionType), Func<ICommand, Exception, ICommand>> _routes = new();

   
    public void Register(Type commandType, Type exceptionType, Func<ICommand, Exception, ICommand> handlerFactory)
    {
        if (commandType == null)
        {
            throw new ArgumentNullException(nameof(commandType));
        }

        if (exceptionType == null)
        {
            throw new ArgumentNullException(nameof(exceptionType));
        }

        _routes[(commandType, exceptionType)] = handlerFactory ?? throw new ArgumentNullException(nameof(handlerFactory));
    }

    
    public ICommand Handle(ICommand command, Exception exception)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        if (exception == null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        var commandTypes = GetTypeHierarchy(command.GetType());
        var exceptionTypes = GetTypeHierarchy(exception.GetType());

        foreach (var cmdType in commandTypes)
        {
            foreach (var exType in exceptionTypes)
            {
                if (_routes.TryGetValue((cmdType, exType), out var handlerFactory))
                {
                    return handlerFactory(command, exception);
                }
            }
        }

        return new LogExceptionCommand(command, exception);
    }

    private static List<Type> GetTypeHierarchy(Type type)
    {
        var result = new List<Type>();
        var visited = new HashSet<Type>();

        for (var current = type; current != null && current != typeof(object); current = current.BaseType)
        {
            if (visited.Add(current))
            {
                result.Add(current);
            }
        }

        // 2. Реализуемые интерфейсы (например, ICommand)
        foreach (var iface in type.GetInterfaces())
        {
            if (visited.Add(iface))
            {
                result.Add(iface);
            }
        }

        if (visited.Add(typeof(object)))
        {
            result.Add(typeof(object));
        }

        return result;
    }
}