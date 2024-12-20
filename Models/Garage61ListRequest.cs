using System.Collections.Generic;
using Newtonsoft.Json;

namespace Garage61Data.Models
{
    public class Garage61ListRequest<T>
    {
        [JsonProperty("items")] public List<T> Items;
        [JsonProperty("total")] public int Total;
    }
}