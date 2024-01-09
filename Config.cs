using BepInEx.Configuration;

namespace RemoteOpenDoor
{
    public class RemoteConfig
    {
        ConfigEntry<bool> CustomConfigValue;

        // Constructor
        public RemoteConfig()
        {
        }

        // Bind config values to fields
        public void BindConfig(ConfigFile _config)
        {
            CustomConfigValue = _config.Bind("General", "CustomConfigValue", true, "A custom configuration value.");
        }

        public bool GetCustomConfigValue()
        {
            return CustomConfigValue.Value;
        }
    }
}
