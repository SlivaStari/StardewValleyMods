using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ContentPatcherAnimations.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceShared;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace ContentPatcherAnimations
{
    internal class PatchData
    {
        public object PatchObj;
        public Func<bool> IsActive;
        public Func<Texture2D> TargetFunc;
        public Texture2D Target;
        public Func<Texture2D> SourceFunc;
        public Texture2D Source;
        public Func<Rectangle> FromAreaFunc;
        public Func<Rectangle> ToAreaFunc;
        public int CurrentFrame;
    }

    internal class ScreenState
    {
        public IEnumerable CpPatches;

        public Dictionary<Patch, PatchData> AnimatedPatches = new();

        public uint FrameCounter;
        public int FindTargetsCounter;
        public Queue<Patch> FindTargetsQueue = new();
    }

    internal class Mod : StardewModdingAPI.Mod
    {
        public static Mod Instance;

        public const BindingFlags PublicI = BindingFlags.Public | BindingFlags.Instance;
        public const BindingFlags PublicS = BindingFlags.Public | BindingFlags.Static;
        public const BindingFlags PrivateI = BindingFlags.NonPublic | BindingFlags.Instance;
        public const BindingFlags PrivateS = BindingFlags.NonPublic | BindingFlags.Static;

        private StardewModdingAPI.Mod ContentPatcher;
        private readonly PerScreen<ScreenState> ScreenStateImpl = new();
        internal ScreenState ScreenState => this.ScreenStateImpl.Value;

        public override void Entry(IModHelper helper)
        {
            Mod.Instance = this;
            Log.Monitor = this.Monitor;

            this.Helper.Events.GameLoop.UpdateTicked += this.UpdateAnimations;

            void UpdateTargets()
            {
                foreach (var screen in this.ScreenStateImpl.GetActiveValues())
                    screen.Value.FindTargetsCounter = 1;
            }

            this.Helper.Events.GameLoop.SaveCreated += (s, e) => UpdateTargets();
            this.Helper.Events.GameLoop.SaveLoaded += (s, e) => UpdateTargets();
            this.Helper.Events.GameLoop.DayStarted += (s, e) => UpdateTargets();

            helper.Content.AssetEditors.Add(new WatchForUpdatesAssetEditor());

            helper.ConsoleCommands.Add("cpa", "...", this.OnCommand);
        }

        private void OnCommand(string cmd, string[] args)
        {
            if (args[0] == "reload")
            {
                this.CollectPatches();
            }
        }

        private void UpdateAnimations(object sender, UpdateTickedEventArgs e)
        {
            if (this.ContentPatcher == null)
            {
                var modData = this.Helper.ModRegistry.Get("Pathoschild.ContentPatcher");
                this.ContentPatcher = (StardewModdingAPI.Mod)modData.GetType().GetProperty("Mod", Mod.PrivateI | Mod.PublicI).GetValue(modData);
            }

            this.ScreenStateImpl.Value ??= new ScreenState();

            if (this.ScreenState.CpPatches == null)
            {
                object screenManagerPerScreen = this.ContentPatcher.GetType().GetField("ScreenManager", Mod.PrivateI).GetValue(this.ContentPatcher);
                object screenManager = screenManagerPerScreen.GetType().GetProperty("Value").GetValue(screenManagerPerScreen);
                object patchManager = screenManager.GetType().GetProperty("PatchManager").GetValue(screenManager);
                this.ScreenStateImpl.Value.CpPatches = (IEnumerable)patchManager.GetType().GetField("Patches", Mod.PrivateI).GetValue(patchManager);

                this.CollectPatches();
            }


            if (this.ScreenState.FindTargetsCounter > 0 && --this.ScreenState.FindTargetsCounter == 0)
                this.UpdateTargetTextures();
            while (this.ScreenState.FindTargetsQueue.Count > 0)
            {
                var patch = this.ScreenState.FindTargetsQueue.Dequeue();
                this.UpdateTargetTextures(patch);
            }

            ++this.ScreenState.FrameCounter;
            Game1.graphics.GraphicsDevice.Textures[0] = null;
            foreach (var patch in this.ScreenState.AnimatedPatches)
            {
                if (!patch.Value.IsActive.Invoke() || patch.Value.Source == null || patch.Value.Target == null)
                    continue;

                try
                {
                    if (this.ScreenState.FrameCounter % patch.Key.AnimationFrameTime == 0)
                    {
                        if (++patch.Value.CurrentFrame >= patch.Key.AnimationFrameCount)
                            patch.Value.CurrentFrame = 0;

                        var sourceRect = patch.Value.FromAreaFunc.Invoke();
                        sourceRect.X += patch.Value.CurrentFrame * sourceRect.Width;
                        var targetRect = patch.Value.ToAreaFunc.Invoke();
                        if (targetRect == Rectangle.Empty)
                            targetRect = new Rectangle(0, 0, sourceRect.Width, sourceRect.Height);
                        var cols = new Color[sourceRect.Width * sourceRect.Height];
                        patch.Value.Source.GetData(0, sourceRect, cols, 0, cols.Length);
                        patch.Value.Target.SetData(0, targetRect, cols, 0, cols.Length);
                    }
                }
                catch
                {
                    // No idea why this happens, hack fix
                    patch.Value.Target = null;
                    this.ScreenState.FindTargetsQueue.Enqueue(patch.Key);
                }
            }
        }

        private void UpdateTargetTextures()
        {
            foreach (var patch in this.ScreenState.AnimatedPatches)
            {
                try
                {
                    if (!patch.Value.IsActive.Invoke())
                        continue;

                    patch.Value.Source = patch.Value.SourceFunc();
                    patch.Value.Target = patch.Value.TargetFunc();
                }
                catch (Exception e)
                {
                    Log.Trace("Exception loading " + patch.Key.LogName + " textures, delaying to try again next frame: " + e);
                    this.ScreenState.FindTargetsQueue.Enqueue(patch.Key);
                }
            }
        }

        private void UpdateTargetTextures(Patch key)
        {
            try
            {
                var patch = this.ScreenState.AnimatedPatches[key];
                if (!patch.IsActive())
                    return;

                patch.Source = patch.SourceFunc();
                patch.Target = patch.TargetFunc();
            }
            catch (Exception e)
            {
                Log.Error("Exception loading " + key.LogName + " textures: " + e);
            }
        }

        private void CollectPatches()
        {
            this.ScreenState.AnimatedPatches.Clear();
            this.ScreenState.FindTargetsQueue.Clear();
            foreach (var pack in this.ContentPatcher.Helper.ContentPacks.GetOwned())
            {
                var patches = pack.ReadJsonFile<PatchList>("content.json");
                foreach (var patch in patches.Changes)
                {
                    if (patch.AnimationFrameTime > 0 && patch.AnimationFrameCount > 0)
                    {
                        Log.Trace("Loading animated patch from content pack " + pack.Manifest.UniqueID);
                        if (string.IsNullOrEmpty(patch.LogName))
                        {
                            Log.Error("Animated patches must specify a LogName!");
                            continue;
                        }

                        PatchData data = new PatchData();

                        object targetPatch = null;
                        foreach (object cpPatch in this.ScreenState.CpPatches)
                        {
                            object path = cpPatch.GetType().GetProperty("Path", Mod.PublicI).GetValue(cpPatch);
                            if (path.ToString() == pack.Manifest.Name + " > " + patch.LogName)
                            {
                                targetPatch = cpPatch;
                                break;
                            }
                        }
                        if (targetPatch == null)
                        {
                            Log.Error("Failed to find patch with name \"" + patch.LogName + "\"!?!?");
                            continue;
                        }
                        var appliedProp = targetPatch.GetType().GetProperty("IsApplied", Mod.PublicI);
                        var sourceProp = targetPatch.GetType().GetProperty("FromAsset", Mod.PublicI);
                        var targetProp = targetPatch.GetType().GetProperty("TargetAsset", Mod.PublicI);

                        data.PatchObj = targetPatch;
                        data.IsActive = () => (bool)appliedProp.GetValue(targetPatch);
                        data.SourceFunc = () => pack.LoadAsset<Texture2D>((string)sourceProp.GetValue(targetPatch));
                        data.TargetFunc = () => this.FindTargetTexture((string)targetProp.GetValue(targetPatch));
                        data.FromAreaFunc = () => this.GetRectangleFromPatch(targetPatch, "FromArea");
                        data.ToAreaFunc = () => this.GetRectangleFromPatch(targetPatch, "ToArea", new Rectangle(0, 0, data.FromAreaFunc().Width, data.FromAreaFunc().Height));

                        this.ScreenState.AnimatedPatches.Add(patch, data);
                    }
                }
            }
        }

        private Texture2D FindTargetTexture(string target)
        {
            if (this.Helper.Content.NormalizeAssetName(target) == this.Helper.Content.NormalizeAssetName("TileSheets\\tools"))
            {
                return this.Helper.Reflection.GetField<Texture2D>(typeof(Game1), "_toolSpriteSheet").GetValue();
            }
            var tex = Game1.content.Load<Texture2D>(target);
            if (tex.GetType().Name == "ScaledTexture2D")
            {
                Log.Trace("Found ScaledTexture2D from PyTK: " + target);
                tex = this.Helper.Reflection.GetProperty<Texture2D>(tex, "STexture").GetValue();
            }
            return tex;
        }

        private Rectangle GetRectangleFromPatch(object targetPatch, string rectName, Rectangle defaultTo = default)
        {
            object rect = targetPatch.GetType().GetField(rectName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(targetPatch);
            if (rect == null)
            {
                return defaultTo;
            }
            var tryGetRectValue = rect.GetType().GetMethod("TryGetRectangle");

            object[] args = new object[] { null, null };
            if (!((bool)tryGetRectValue.Invoke(rect, args)))
            {
                return Rectangle.Empty;
            }

            return (Rectangle)args[0];
        }
    }
}
