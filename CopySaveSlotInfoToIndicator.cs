using Kitchen;
using KitchenMods;
using Unity.Collections;
using Unity.Entities;

namespace KitchenSaveSlotInfo
{
    public class CopySaveSlotInfoToIndicator : GameSystemBase, IModSystem
    {
        EntityQuery LocationChoicesIndicators;

        protected override void Initialise()
        {
            base.Initialise();
            LocationChoicesIndicators = GetEntityQuery(new QueryHelper()
                .All(typeof(CGenericInputIndicator), typeof(CIndicator))
                .None(typeof(CModItem)));
        }

        protected override void OnUpdate()
        {
            using NativeArray<Entity> entities = LocationChoicesIndicators.ToEntityArray(Allocator.Temp);
            using NativeArray<CIndicator> indicators = LocationChoicesIndicators.ToComponentDataArray<CIndicator>(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                Entity source = indicators[i].Source;
                if (source == default ||
                    !Has<CLocationChoice>(source) ||
                    !RequireBuffer(source, out DynamicBuffer<CModItem> sourceBuffer))
                    continue;

                DynamicBuffer<CModItem> buffer = EntityManager.AddBuffer<CModItem>(entities[i]);
                buffer.CopyFrom(sourceBuffer);
            }
        }
    }
}
