using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WolvenKit.CR2W;
using Newtonsoft.Json;
using WolvenKit.CR2W.Types;

namespace WolvenKit.CR2W.JSON
{
    public abstract class CR2WJsonObject
    {
        #region Properties
        [JsonProperty("_type", Order = -2)]
        public string type { get; set; }
        #endregion
        public CR2WJsonObject() { }
        public CR2WJsonObject(string _type)
        {
            this.type = _type;
        }
    }
    public class CR2WJsonScalar : CR2WJsonObject
    {
        #region Properties
        [JsonProperty("_value")]
        public object value { get; set; }
        #endregion
        public CR2WJsonScalar() { }
        public CR2WJsonScalar(string _type) : base(_type)
        {
            value = null;
        }
        public CR2WJsonScalar(string _type, object _val) : base(_type)
        {
            value = _val;
        }
    }
    public class CR2WJsonArray : CR2WJsonObject
    {
        #region Properties
        [JsonProperty("_bufferPadding")]
        public float? bufferPadding { get; set; }
        [JsonProperty("_elements")]
        public List<CR2WJsonObject> elements { get; set; }
        #endregion
        public CR2WJsonArray() { }
        public CR2WJsonArray(string _type) : base(_type)
        {
            elements = new List<CR2WJsonObject>();
            bufferPadding = null;  // won't be serialized without need
        }
    }
    public class CR2WJsonMap : CR2WJsonObject
    {
        #region Properties
        [JsonProperty("_vars")]
        public Dictionary<string, CR2WJsonObject> vars { get; set; }
        #endregion
        public CR2WJsonMap() { }
        public CR2WJsonMap(string _type) : base(_type)
        {
            vars = new Dictionary<string, CR2WJsonObject>();
        }
    }
    public class CR2WJsonChunkMap : CR2WJsonObject
    {
        #region Properties
        [JsonProperty("_key")]
        public string chunkKey { get; set; }
        [JsonProperty("_parentKey")]
        public string parentKey { get; set; }
        [JsonProperty("_flags")]
        public ushort flags { get; set; }
        [JsonProperty("_unknownBytes")]
        public byte[] unknownBytes { get; set; }
        [JsonProperty("_vars")]
        public Dictionary<string, CR2WJsonObject> vars { get; set; }
        #endregion
        public CR2WJsonChunkMap() { }
        public CR2WJsonChunkMap(string _type) : base(_type)
        {
            flags = 0;  // 0 == uncooked
			unknownBytes = null;  // won't be serialized without need
            vars = new Dictionary<string, CR2WJsonObject>();
        }
    }
    public class CR2WJsonData : CR2WJsonObject
    {
        #region Properties
        [JsonProperty("_extension")]
        public string extension { get; set; }
        [JsonProperty("_imports")]
        public List<Dictionary<string, object>> imports { get; set; }
        [JsonProperty("_properties")]
        public List<CR2WPropertyWrapper> properties { get; set; }
        [JsonProperty("_buffers")]
        public List<CR2WBufferWrapper> buffers { get; set; }
        [JsonProperty("_embedded")]
        public List<Dictionary<string, object>> embedded { get; set; }
        [JsonProperty("_additionalBytes")]
        public byte[] additionalBytes { get; set; }
        [JsonProperty("_chunks")]
        public Dictionary<string, CR2WJsonChunkMap> chunks { get; set; }
        #endregion
        public CR2WJsonData() { }
        public CR2WJsonData(string _extension) : base("CR2W")
        {
            chunks = new Dictionary<string, CR2WJsonChunkMap>();
            imports = new List<Dictionary<string, object>>();
            properties = new List<CR2WPropertyWrapper>();
            buffers = new List<CR2WBufferWrapper>();
            embedded = new List<Dictionary<string, object>>();
            extension = _extension;
            additionalBytes = null;  // won't be serialized without need
        }
    }
}
