using System.Collections.Generic;
using Shop;

namespace Souvenir
{
    public interface ISouvenirPipelineContext
    {
    }

    public interface ISouvenirPipelineHandler<TContext>
        where TContext : class, ISouvenirPipelineContext
    {
        void Apply(TContext context);
    }

    public sealed class ShopShelfPipelineContext : ISouvenirPipelineContext
    {
        private readonly Dictionary<int, ShelfSlotVisualInfo> _visualInfosBySlotIndex;

        public ShopShelfPipelineContext(string shopId, List<ShelfSlot> items, bool buildVisualInfos)
        {
            ShopId = shopId;
            Items = items ?? new List<ShelfSlot>();
            VisualInfos = new List<ShelfSlotVisualInfo>();
            _visualInfosBySlotIndex = new Dictionary<int, ShelfSlotVisualInfo>();

            if (buildVisualInfos)
            {
                BuildVisualInfos();
            }
        }

        public string ShopId { get; }
        public List<ShelfSlot> Items { get; }
        public List<ShelfSlotVisualInfo> VisualInfos { get; }

        public bool TryGetVisualInfo(int slotIndex, out ShelfSlotVisualInfo info)
        {
            return _visualInfosBySlotIndex.TryGetValue(slotIndex, out info);
        }

        private void BuildVisualInfos()
        {
            foreach (var slot in Items)
            {
                if (slot == null) continue;

                var info = new ShelfSlotVisualInfo
                {
                    SlotIndex = slot.SlotIndex
                };

                VisualInfos.Add(info);
                _visualInfosBySlotIndex[slot.SlotIndex] = info;
                slot.VisualInfo = info;
            }
        }
    }

    public sealed class BagCapacityPipelineContext : ISouvenirPipelineContext
    {
        public int ExtraCapacity { get; private set; }

        public void AddExtraCapacity(int value)
        {
            if (value > 0)
            {
                ExtraCapacity += value;
            }
        }
    }

    public sealed class ScratchCardPipelineContext : ISouvenirPipelineContext
    {
        public bool IsFree { get; private set; }

        public void MarkFree()
        {
            IsFree = true;
        }
    }
}
