using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WolvenKit.CR2W.Types;

namespace WolvenKit.CR2W
{
    public class CR2WScripts
    {
        public const int DumpClassname = 1;
        public const int DumpImports = 2;
        public CR2WScripts()
        {
        }

        public static CR2WFile LoadCR2WFile(string fullpath)
        {
            using (var fs = new FileStream(fullpath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(fs))
            {
                // read main cr2w
                var cr2w = new CR2WFile();
                var errcode = cr2w.Read(reader);
                return cr2w;
            }
        }

        public static bool DumpInfo(string cr2wPath, string savePath, int dumpFlags = DumpClassname | DumpImports)
        {
            var info = new Dictionary<string, object>();
            var cr2w = LoadCR2WFile(cr2wPath);
            if (cr2w == null)
                return false;

            info["path"] = cr2wPath.Replace('\\', '/');
            if ((dumpFlags & DumpClassname) > 0 && cr2w.chunks.Count > 0)
            {
                info["class"] = cr2w.chunks[0].REDType;
                if (cr2w.chunks[0].data is CEntityTemplate template && template.EntityObject.Reference != null)
                {
                    info["entityClass"] = template.EntityObject.Reference.REDType;
                }
            }
            if ((dumpFlags & DumpImports) > 0)
            {
                var imports = new List<string>();
                foreach (var i in cr2w.imports)
                {
                    imports.Add(i.DepotPathStr.Replace('\\', '/'));
                }
                imports.Sort();
                info["imports"] = imports;
            }

            var serializeSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
                NullValueHandling = NullValueHandling.Ignore
            };

            var jsonText = JsonConvert.SerializeObject(info, Newtonsoft.Json.Formatting.Indented, serializeSettings);
            File.WriteAllText(savePath, jsonText);
            return true;
        }
    }
}
