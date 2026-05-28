using System;
using System.Collections;
using System.Collections.Generic;

namespace Souvenir
{
    public sealed class SouvenirEffectRegistry
    {
        private static readonly Type PipelineHandlerTypeDefinition = typeof(ISouvenirPipelineHandler<>);

        private readonly Dictionary<Type, object> _ownedEffectsByType = new Dictionary<Type, object>();
        private readonly Dictionary<Type, object> _allSpecialEffectsByType = new Dictionary<Type, object>();
        private readonly Dictionary<Type, object> _pipelineHandlersByContextType = new Dictionary<Type, object>();

        public void Rebuild(IEnumerable<SouvenirBase> ownedSouvenirs, IEnumerable<SpecialSouvenir> allSpecialSouvenirs)
        {
            _ownedEffectsByType.Clear();
            _allSpecialEffectsByType.Clear();
            _pipelineHandlersByContextType.Clear();

            if (ownedSouvenirs != null)
            {
                foreach (var souvenir in ownedSouvenirs)
                {
                    RegisterOwnedSouvenir(souvenir);
                }
            }

            if (allSpecialSouvenirs != null)
            {
                foreach (var souvenir in allSpecialSouvenirs)
                {
                    RegisterAllSpecialSouvenir(souvenir);
                }
            }
        }

        public IReadOnlyList<T> GetOwned<T>() where T : class
        {
            return GetList<T>(_ownedEffectsByType);
        }

        public IReadOnlyList<T> GetAllSpecial<T>() where T : class
        {
            return GetList<T>(_allSpecialEffectsByType);
        }

        public IReadOnlyList<ISouvenirPipelineHandler<TContext>> GetPipelineHandlers<TContext>()
            where TContext : class, ISouvenirPipelineContext
        {
            return GetList<ISouvenirPipelineHandler<TContext>>(_pipelineHandlersByContextType, typeof(TContext));
        }

        private void RegisterOwnedSouvenir(SouvenirBase souvenir)
        {
            if (souvenir == null) return;

            AddIfImplemented<IApplyStartEffect>(_ownedEffectsByType, souvenir);
            AddIfImplemented<IDailyEffect>(_ownedEffectsByType, souvenir);
            AddIfImplemented<IShopPurchaseListener>(_ownedEffectsByType, souvenir);
            AddIfImplemented<IMonsterTradeListener>(_ownedEffectsByType, souvenir);
            AddPipelineHandlers(souvenir);
        }

        private void RegisterAllSpecialSouvenir(SpecialSouvenir souvenir)
        {
            if (souvenir == null) return;

            AddIfImplemented<IMonsterTradeWithRaceListener>(_allSpecialEffectsByType, souvenir);
            AddIfImplemented<IMonsterTradeFailedListener>(_allSpecialEffectsByType, souvenir);
        }

        private void AddPipelineHandlers(SouvenirBase souvenir)
        {
            foreach (var interfaceType in souvenir.GetType().GetInterfaces())
            {
                if (!interfaceType.IsGenericType ||
                    interfaceType.GetGenericTypeDefinition() != PipelineHandlerTypeDefinition)
                {
                    continue;
                }

                var contextType = interfaceType.GetGenericArguments()[0];
                if (!typeof(ISouvenirPipelineContext).IsAssignableFrom(contextType))
                {
                    continue;
                }

                AddPipelineHandler(contextType, interfaceType, souvenir);
            }
        }

        private void AddPipelineHandler(Type contextType, Type handlerInterfaceType, object handler)
        {
            if (!_pipelineHandlersByContextType.TryGetValue(contextType, out var boxedList))
            {
                var listType = typeof(List<>).MakeGenericType(handlerInterfaceType);
                boxedList = Activator.CreateInstance(listType);
                _pipelineHandlersByContextType[contextType] = boxedList;
            }

            ((IList)boxedList).Add(handler);
        }

        private static void AddIfImplemented<T>(Dictionary<Type, object> map, object source)
            where T : class
        {
            if (source is T target)
            {
                Add(map, typeof(T), target);
            }
        }

        private static void Add<T>(Dictionary<Type, object> map, Type key, T value)
            where T : class
        {
            if (!map.TryGetValue(key, out var boxedList))
            {
                boxedList = new List<T>();
                map[key] = boxedList;
            }

            ((List<T>)boxedList).Add(value);
        }

        private static IReadOnlyList<T> GetList<T>(Dictionary<Type, object> map)
            where T : class
        {
            return GetList<T>(map, typeof(T));
        }

        private static IReadOnlyList<T> GetList<T>(Dictionary<Type, object> map, Type key)
            where T : class
        {
            if (map.TryGetValue(key, out var boxedList))
            {
                return (List<T>)boxedList;
            }

            return Array.Empty<T>();
        }
    }
}
