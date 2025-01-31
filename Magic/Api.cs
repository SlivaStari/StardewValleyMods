using System;
using SpaceShared;
using StardewValley;

namespace Magic
{
    public interface IApi
    {
        event EventHandler OnAnalyzeCast;
    }

    public class Api : IApi
    {
        public event EventHandler OnAnalyzeCast;
        internal void InvokeOnAnalyzeCast(Farmer farmer)
        {
            Log.Trace("Event: OnAnalyzeCast");
            if (this.OnAnalyzeCast == null)
                return;
            Util.InvokeEvent("Magic.Api.OnAnalyzeCast", this.OnAnalyzeCast.GetInvocationList(), farmer);
        }
    }
}
