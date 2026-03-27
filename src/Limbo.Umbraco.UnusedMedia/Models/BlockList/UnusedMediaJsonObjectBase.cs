using Newtonsoft.Json.Linq;
using Skybrud.Essentials.Json.Newtonsoft;

namespace Limbo.Umbraco.UnusedMedia.Models.BlockList;

public class UnusedMediaJsonObjectBase : JsonObjectBase {

    protected UnusedMediaJsonObjectBase(JObject json) : base(json) { }

}