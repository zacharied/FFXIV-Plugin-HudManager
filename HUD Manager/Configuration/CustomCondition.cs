using Newtonsoft.Json;
using System;

namespace HUDManager.Configuration
{
    [Serializable]
    [JsonObject(IsReference = true)]
    public class CustomCondition
    {
        public string Name { get; set; }

        public CustomCondition(string name)
        {
            Name = name;
        }
    }
}