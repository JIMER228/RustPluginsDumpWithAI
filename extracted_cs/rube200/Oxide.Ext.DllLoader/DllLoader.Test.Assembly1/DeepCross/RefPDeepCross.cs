// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using DllLoader.Test.Libs;

namespace Oxide.Plugins
{
    [Info("RefPDeepCross", "Rube200", "1.0.0")]
    [Description("RefPDeepCross is for testing")]
    public class RefPDeepCross : TestPlugin
    {
        protected override void Init()
        {
            base.Init();
        }

        protected override void Loaded()
        {
            base.Loaded();
        }

        protected override void Unload()
        {
            base.Unload();
        }

        protected override void Shutdown()
        {
            base.Shutdown();
        }

        protected override void Hotloading()
        {
            base.Hotloading();
        }

        protected override void CallRef(string callerMsg)
        {
            base.CallRef(callerMsg);
        }
    }
}