using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using SpaceShared;

namespace JsonAssets.Data
{
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = DiagnosticMessages.IsPublicApi)]
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = DiagnosticMessages.IsPublicApi)]
    public class FenceData : DataNeedsIdWithTexture
    {
        [JsonIgnore]
        public Texture2D objectTexture;

        [JsonIgnore]
        internal ObjectData correspondingObject;

        public enum ToolType
        {
            Axe,
            Pickaxe,
        }

        public class Recipe_
        {
            public class Ingredient
            {
                public object Object { get; set; }
                public int Count { get; set; }
            }

            public string SkillUnlockName { get; set; } = null;
            public int SkillUnlockLevel { get; set; } = -1;

            public int ResultCount { get; set; } = 1;
            public IList<Ingredient> Ingredients { get; set; } = new List<Ingredient>();

            public bool IsDefault { get; set; } = false;
            public bool CanPurchase { get; set; } = false;
            public int PurchasePrice { get; set; }
            public string PurchaseFrom { get; set; } = "Robin";
            public IList<string> PurchaseRequirements { get; set; } = new List<string>();
            public IList<PurchaseData> AdditionalPurchaseData { get; set; } = new List<PurchaseData>();
        }

        public string Description { get; set; }

        public int MaxHealth { get; set; } = 1;
        public object RepairMaterial { get; set; }
        public ToolType BreakTool { get; set; }
        public string PlacementSound { get; set; }
        public string RepairSound { get; set; }

        public int Price { get; set; }
        public Recipe_ Recipe { get; set; }

        public bool CanPurchase { get; set; } = false;
        public int PurchasePrice { get; set; }
        public string PurchaseFrom { get; set; } = "Robin";
        public IList<string> PurchaseRequirements { get; set; } = new List<string>();
        public IList<PurchaseData> AdditionalPurchaseData { get; set; } = new List<PurchaseData>();

        public Dictionary<string, string> NameLocalization = new();
        public Dictionary<string, string> DescriptionLocalization = new();

        public int GetObjectId() { return this.Id; }
    }
}
