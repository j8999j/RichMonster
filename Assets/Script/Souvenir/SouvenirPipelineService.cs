using System.Collections.Generic;
using Shop;

namespace Souvenir
{
    public sealed class SouvenirPipelineService
    {
        private readonly SouvenirEffectRegistry _registry;

        public SouvenirPipelineService(SouvenirEffectRegistry registry)
        {
            _registry = registry;
        }

        public List<ShelfSlotVisualInfo> ApplyShopShelf(string shopId, List<ShelfSlot> items, bool buildVisualInfos)
        {
            var context = new ShopShelfPipelineContext(shopId, items, buildVisualInfos);
            Run(context);
            return context.VisualInfos;
        }

        public int CalculateExtraBagCapacity()
        {
            var context = new BagCapacityPipelineContext();
            Run(context);
            return context.ExtraCapacity;
        }

        public bool EvaluateScratchCardFree()
        {
            var context = new ScratchCardPipelineContext();
            Run(context);
            return context.IsFree;
        }

        private void Run<TContext>(TContext context)
            where TContext : class, ISouvenirPipelineContext
        {
            if (_registry == null) return;

            foreach (var handler in _registry.GetPipelineHandlers<TContext>())
            {
                handler.Apply(context);
            }
        }
    }
}
