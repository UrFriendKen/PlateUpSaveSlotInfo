using Kitchen;
using Kitchen.Modules;
using KitchenMods;
using ModContentCache;
using MessagePack;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace KitchenSaveSlotInfo
{
    public class SaveSlotInfoView : UpdatableObjectView<SaveSlotInfoView.ViewData>
    {
        public class UpdateView : IncrementalViewSystemBase<ViewData>, IModSystem
        {
            EntityQuery Views;

            protected override void Initialise()
            {
                base.Initialise();
                Views = GetEntityQuery(typeof(CGenericInputIndicator), typeof(CIndicator), typeof(CLinkedView), typeof(CModItem));
            }

            protected override void OnUpdate()
            {
                using NativeArray<Entity> entities = Views.ToEntityArray(Allocator.Temp);
                using NativeArray<CLinkedView> views = Views.ToComponentDataArray<CLinkedView>(Allocator.Temp);

                for (int i = 0; i < views.Length; i++)
                {
                    List<ulong> modIDs = new List<ulong>();
                    DynamicBuffer<CModItem> buffer = GetBuffer<CModItem>(entities[i]);

                    for (int j = 0; j < buffer.Length; j++)
                    {
                        modIDs.Add(buffer[j].ModID);
                    }

                    SendUpdate(views[i], new ViewData()
                    {
                        RequiredMods = modIDs
                    });
                }
            }
        }

        [MessagePackObject(false)]
        public class ViewData : ISpecificViewData, IViewData.ICheckForChanges<ViewData>
        {
            [Key(0)] public List<ulong> RequiredMods;

            public IUpdatableObject GetRelevantSubview(IObjectView view) => view.GetSubView<SaveSlotInfoView>();

            public bool IsChangedFrom(ViewData check)
            {
                return RequiredMods.Count != check.RequiredMods.Count ||
                    RequiredMods.Intersect(check.RequiredMods).Count() != RequiredMods.Count;
            }
        }

        private PanelElement Panel;

        public Transform Anchor;

        public Transform Container;

        private ModuleList ModuleList = new ModuleList();

        public virtual ElementStyle Style { get; set; }

        protected Vector2 DefaultElementSize = new Vector2(3.5f, 0.5f);

        private static FieldInfo f_LabelElement_Label = typeof(LabelElement).GetField("Label", BindingFlags.NonPublic | BindingFlags.Instance);

        protected override void UpdateData(ViewData data)
        {
            InitPanel();
            ModuleList.Clear();

            IEnumerable<ulong> subscribedMods = ModPreload.Mods.Select(mod => mod.ID);

            IEnumerable<ulong> missingModIDs = data.RequiredMods.Where(modID => !subscribedMods.Contains(modID));
            if (missingModIDs.Count() > 0)
            {
                AddLabel($"{missingModIDs.Count()} Missing Mods");
                New<SpacerElement>();
                foreach (ulong modID in missingModIDs)
                {
                    AddLabel(ModInfoRegistry.GetModName(modID), new Color(0.34f, 0.36f, 0.42f));
                }
            }
            SetContainerPosition();
            Panel.SetTarget(ModuleList.Modules.Any() ? ModuleList : null);
        }

        private void SetContainerPosition()
        {
            Container.localPosition = (Anchor?.localPosition ?? Vector3.zero) - ModuleList.BoundingBox.center;
        }

        protected virtual LabelElement AddLabel(string text)
        {
            LabelElement labelElement = New<LabelElement>();
            labelElement.SetStyle(Style);
            labelElement.SetSize(DefaultElementSize.x, DefaultElementSize.y);
            labelElement.SetLabel(text);
            return labelElement;
        }

        protected virtual LabelElement AddLabel(string text, Color color)
        {
            LabelElement labelElement = AddLabel(text);
            TextMeshPro tmp = (TextMeshPro)f_LabelElement_Label?.GetValue(labelElement);
            if (tmp != null)
            {
                tmp.color = color;
            }
            return labelElement;
        }

        protected virtual InfoBoxElement AddInfo(string text)
        {
            InfoBoxElement infoBoxElement = New<InfoBoxElement>();
            infoBoxElement.SetSize(DefaultElementSize.x, infoBoxElement.BoundingBox.size.y);
            infoBoxElement.SetLabel(text);
            infoBoxElement.SetStyle(Style);
            return infoBoxElement;
        }

        protected virtual TElement New<TElement>(bool add_to_module_list = true) where TElement : Element
        {
            TElement val = ModuleDirectory.Add<TElement>(Container);
            if (add_to_module_list)
            {
                ModuleList.AddModule(val);
            }
            return val;
        }

        private void InitPanel()
        {
            if (Panel == null)
            {
                Panel = ModuleDirectory.Add<PanelElement>(Container);
            }
        }
    }
}
