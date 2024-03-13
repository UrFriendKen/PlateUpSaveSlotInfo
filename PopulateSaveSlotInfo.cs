using Kitchen;
using KitchenMods;
using ModContentCache;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEngine;

namespace KitchenSaveSlotInfo
{
    [InternalBufferCapacity(8)]
    public struct CModItem : IBufferElementData
    {
        public ulong ModID;
    }

    public class PopulateSaveSlotInfo : GameSystemBase, IModSystem
    {
        EntityQuery LocationChoices;

        EntityQuery SaveSlotInfos;

        protected override void Initialise()
        {
            base.Initialise();
            LocationChoices = GetEntityQuery(new QueryHelper()
                .All(typeof(CLocationChoice))
                .None(typeof(CModItem)));
            SaveSlotInfos = GetEntityQuery(typeof(CModItem));

            RequireForUpdate(LocationChoices);
        }

        public override void BeforeSaving(SaveSystemType system_type)
        {
            base.BeforeSaving(system_type);
            EntityManager.RemoveComponent<CModItem>(SaveSlotInfos);
        }

        protected override void OnUpdate()
        {
            using NativeArray<Entity> entities = LocationChoices.ToEntityArray(Allocator.Temp);
            using NativeArray<CLocationChoice> locationChoices = LocationChoices.ToComponentDataArray<CLocationChoice>(Allocator.Temp);

            HashSet<ulong> componentHashes = new HashSet<ulong>();
            for (int i = 0; i < entities.Length; i++)
            {
                DynamicBuffer<CModItem> buffer = EntityManager.AddBuffer<CModItem>(entities[i]);
                CLocationChoice locationChoice = locationChoices[i];

                string directory = Dirpath(locationChoice.Slot);

                if (!Directory.Exists(directory))
                    continue;

                IEnumerable<string> filepaths = new DirectoryInfo(directory).GetFiles()
                    .Where(f => f.Extension == ".plateupsave" && f.Length != 0)
                    .OrderByDescending(f => f.LastWriteTime)
                    .Select(f => f.FullName);

                if (!filepaths.Any())
                    continue;

                componentHashes.Clear();
                foreach (string filepath in filepaths)
                {
                    try
                    {
                        using StreamBinaryReader reader = new StreamBinaryReader(filepath, 65536L);
                        int version = BinaryReaderExtensions.ReadInt(reader);
                        if (version != SerializeUtility.CurrentFileFormatVersion)
                        {
                            throw new ArgumentException($"Attempting to read a entity scene stored in an old file format version (stored version : {version}, current version : {SerializeUtility.CurrentFileFormatVersion})");
                        }

                        int componentCount = BinaryReaderExtensions.ReadInt(reader);
                        NativeArray<ulong> stableTypeHashes = new NativeArray<ulong>(componentCount, Allocator.Temp);
                        BinaryReaderExtensions.ReadArray(reader, stableTypeHashes, componentCount);
                        for (int j = 0; j < componentCount; j++)
                        {
                            componentHashes.Add(stableTypeHashes[j]);
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        Main.LogError($"{ex.Message}\n {ex.StackTrace}");
                        continue;
                    }
                }

                if (!ModInfoRegistry.FindUnityTypeHashSource(componentHashes, out var modMetadatas))
                    continue;

                foreach (ulong modId in modMetadatas.Select(modMetadata => modMetadata.ID))
                {
                    buffer.Add(new CModItem()
                    {
                        ModID = modId
                    });
                }
            }
        }

        private string Dirpath(int slot)
        {
            return Path.Combine(Application.persistentDataPath, "Full", slot.ToString());
        }
    }
}
