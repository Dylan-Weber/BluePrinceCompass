using MelonLoader;

[assembly: MelonInfo(typeof(BluePrinceCompass.Core), "Blue Prince Compass Mod", "1.0.0", "ComplexSimple")]
[assembly: MelonGame("Dogubomb", "BLUE PRINCE")]

namespace BluePrinceCompass
{
    public class Core : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Initialized.");
        }
    }
}