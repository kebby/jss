using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Globalization;
using System.Runtime.Serialization;

namespace jssedit
{
    /// <summary>
    /// Definition for a synth module
    /// </summary>
    [Serializable]
    public class ModuleDefinition
    {
        /// <summary>
        /// Name of module. Can be a path (like "Osc/Sawtooth") that will be converted to submenus in the UI
        /// </summary>
        public string Name;

        /// <summary>
        /// Number of input channels (0: no input. 1: mono, 2: stereo)
        /// </summary>
        public int InChannels; // 0,1,2

        /// <summary>
        /// Number of output channels (0: no input. 1: mono, 2: stereo)
        /// </summary>
        public int OutChannels; // 0,1,2

        /// <summary>
        /// Names of parameters
        /// </summary>
        public string[] ParamNames;

        /// <summary>
        /// Default values of parameters (can be omitted but must be same size as ParamNames otherwise)
        /// </summary>
        public float[] ParamDefaults;

        /// <summary>
        /// # of float buffer entries the code needs to run (>=0)
        /// </summary>
        public int WorkspaceSize;

        /// <summary>
        /// JavaScript code for executing the module
        /// </summary>
        public string Code;

        /// <summary>
        /// Global registry of modules
        /// </summary>
        static public readonly Dictionary<string, ModuleDefinition> Registry = new Dictionary<string, ModuleDefinition>();

        /// <summary>
        /// Load modules from JSON definition
        /// </summary>
        /// <param name="json">JSON string</param>
        static public void LoadRegistry(string json)
        {
            var modules = new JavaScriptSerializer().Deserialize<ModuleDefinition[]>(json);
            foreach (var m in modules)
                Registry[m.Name] = m;
        }

        // for debugging
        public override string ToString() { return Name; }
    }


    /// <summary>
    /// Instance of a synth module
    /// </summary>
    [Serializable]
    public class Module
    {
        /// <summary>
        /// Name of module definition
        /// </summary>
        public string DefinitionName;

        /// <summary>
        /// Reference to module definition
        /// </summary>
        [NonSerialized]
        public ModuleDefinition Definition;

        /// <summary>
        /// Input links (first one is always input (even if there's no actual input), entries 1.. are params)
        /// </summary>
        public readonly Module[] Inputs;

        /// <summary>
        /// Parameter values
        /// </summary>
        public readonly float[] Params;

        /// <summary>
        /// UI only: Name of this specific module
        /// </summary>
        public string Name;

        /// <summary>
        /// UI only: Screen position of this module
        /// </summary>
        public int X, Y;

        /// <summary>
        /// Construct a new module from a definition
        /// </summary>
        /// <param name="def">module definition</param>
        public Module(ModuleDefinition def)
        {
            DefinitionName = def.Name;
            Definition = def;

            Params = new float[def.ParamNames.Length];
            if (def.ParamDefaults != null)
                for (int i = 0; i < Math.Min(Params.Length, def.ParamDefaults.Length); i++)
                    Params[i] = def.ParamDefaults[i];

            Inputs = new Module[Params.Length + 1];
            Name = def.Name.Substring(def.Name.LastIndexOf('/')+1);
        }


        [OnDeserialized]
        internal void OnDeserialisation(StreamingContext context)
        {
            Definition = ModuleDefinition.Registry[DefinitionName];
        }


        // for debugging
        public override string ToString() { return Name + ": " + Definition; }
    }

    /// <summary>
    /// Module instance graph
    /// </summary>
    [Serializable]
    public class Graph
    {
        /// <summary>
        /// "Out" module aka root of the graph
        /// </summary>
        public readonly Module Out;

        /// <summary>
        /// List of all modules in the graph (can also contain unconnected ones)
        /// </summary>
        public readonly List<Module> Modules;

        /// <summary>
        /// UI: Name of the graph (aka synth patch)
        /// </summary>
        public string Name;

        /// <summary>
        /// Construct a new graph
        /// </summary>
        public Graph()
        {
            Out = new Module(ModuleDefinition.Registry["!out"]);
            Out.Name = "Output";
            Modules = new List<Module>();
            Modules.Add(Out);
        }


        /// <summary>
        /// Add module to graph
        /// </summary>
        /// <param name="defName">definition name</param>
        public Module AddModule(string defName)
        {
            ModuleDefinition def;
            if (!ModuleDefinition.Registry.TryGetValue(defName, out def))
                throw new ModelException("Module '{0}' not found", defName);

            var module = new Module(def);
            Modules.Add(module);            
            // TODO: set x/y somehow
            return module;
        }


        /// <summary>
        /// Remove module from graph (also disconnects module from everything)
        /// </summary>
        /// <param name="module">module to remove</param>
        public void RemoveModule(Module module)
        {
            if (!Modules.Remove(module)) throw new ModelException("Module not in graph");

            // disconnect from everything
            foreach (var m in Modules)
            {
                for (int i=0; i<m.Inputs.Length; i++)
                    if (m.Inputs[i] == module)
                        m.Inputs[i] = null;
            }

            // ... and before anyone does anything funny...
            Array.Clear(module.Inputs, 0, module.Inputs.Length);
        }


        /// <summary>
        /// Connect output of one module to the input of another
        /// </summary>
        /// <param name="from">Module whose output to connect</param>
        /// <param name="to">Module whose input/parameter to connect</param>
        /// <param name="paramName">name of parameter to connect to, or null for the module's input</param>
        public void Connect(Module from, Module to, string paramName=null)
        {
            if (!Modules.Contains(from)) throw new ModelException("Sender module not in graph");
            if (!Modules.Contains(to))   throw new ModelException("Receiver module not in graph");
            if (from.Definition.OutChannels == 0) throw new ModelException("Module {0} has no output", from.Name);

            if (paramName == null) // connect to input
            {
                if (to.Definition.InChannels == 0) throw new ModelException("Module {0} has no input", to.Name);                
                if (to.Definition.InChannels != from.Definition.OutChannels) throw new ModelException("Module {0} needs {1} input channels", to.Name, to.Definition.InChannels);
                if (to.Inputs[0] != null) throw new ModelException("Input of {0} is already connected", to.Name);
                to.Inputs[0] = from;
            }
            else // connect to parameter
            {
                if (from.Definition.OutChannels != 1) throw new ModelException("Module {0} needs 1 output channel", from.Name);

                var slot = Array.IndexOf(to.Definition.ParamNames, paramName)+1;
                if (slot <= 0) throw new ModelException("Module {0} has no parameter {1}", to.Name, paramName);
                if (to.Inputs[slot] != null) throw new ModelException("Param {0} of {1} is already connected", paramName, to.Name);
                to.Inputs[slot] = from;
            }
        }

        /// <summary>
        /// Remove connection between two modules
        /// </summary>
        /// <param name="to">Module whose input/parameter is connected</param>
        /// <param name="paramName">name of parameter to disconnect, or null for the module's input</param>
        public void Disconnect(Module to, string paramName=null)
        {
            if (!Modules.Contains(to)) throw new ModelException("Receiver module not in graph");

            if (paramName == null) // disconnect from input
            {
                if (to.Definition.InChannels == 0) throw new ModelException("Module {0} has no input", to.Name);
                to.Inputs[0] = null;
            }
            else // disconnect from parameter
            {
                var slot = Array.IndexOf(to.Definition.ParamNames, paramName) + 1;
                if (slot <= 0) throw new ModelException("Module {0} has no parameter {1}", to.Name, paramName);
                to.Inputs[slot] = null;
            }
        }


        public void GenerateCode(out string values, out string code)
        {
            var sb = new StringBuilder();

            // generate topographically sorted node list
            var nodes = new List<ModuleNode>(Modules.Count);
            MakeNodes(Out, nodes);
            nodes.Reverse();

            // determine if output buffering is necessary (main input isn't output of preceding module)
            ModuleNode lastNode = null;
            foreach (var n in nodes)
            {
                var inp = n.Mod.Inputs[0];
                if (inp != null && inp != lastNode.Mod)
                    nodes.First(mn => mn.Mod == inp).OutBuffer = true;
                lastNode = n;
            }

            // determine array positions
            int pos = 0;
            foreach (var n in nodes)
            {
                n.ArrayPos = pos;
                n.ArrayCount = n.Mod.Definition.ParamNames.Length + n.Mod.Definition.WorkspaceSize + (n.OutBuffer ? n.Mod.Definition.OutChannels : 0);
                pos += n.ArrayCount;
            }

            // generate float array
            var vals = new List<string>();
            foreach (var n in nodes)
            {
                var m = n.Mod;
                for (int i = 0; i < m.Params.Length; i++)
                    if (m.Inputs[i + 1] == null)
                        vals.Add(String.Format(CultureInfo.InvariantCulture,"{0}", m.Params[i]));
                    else if (nodes.IndexOf(n) > nodes.IndexOf(nodes.First(mn => mn.Mod == m.Inputs[i+1])))
                        vals.Add("");
                    else
                        vals.Add("0");

                for (int i=0; i < m.Definition.WorkspaceSize + (n.OutBuffer ? m.Definition.OutChannels : 0); i++)
                    vals.Add("0");
            }

            // shorten float numbers
            for (int i = 0; i < vals.Count; i++)
            {
                if (!String.IsNullOrEmpty(vals[i]))
                {
                    var pt = vals[i].IndexOf('.');
                    if (pt >= 0 && vals[i].Substring(0, pt) == "0")
                        vals[i] = vals[i].Substring(pt);
                    //  if(!vals[i].Contains('.'))
                    //      vals[i] += '.';
                }
            }

            values = String.Format("[{0}]", String.Join(",", vals));

            // write code
            lastNode = null;
            foreach (var n in nodes)
            {
                var m = n.Mod;
                //sb.AppendFormat("\n// {0}\n",m.Name);

                // input wiring if necessary
                var inp = m.Inputs[0];
                if (inp != null && inp != lastNode.Mod)
                {
                    var inode = nodes.First(mn => mn.Mod == inp);
                    var offs = (inode.ArrayPos + inode.ArrayCount - inode.Mod.Definition.OutChannels)-n.ArrayPos;
                    sb.AppendFormat("l=v[i+{0}];\n", offs);
                    if (m.Definition.InChannels==2)
                        sb.AppendFormat("r=v[i+{0}];\n", offs+1);
                }

                // execute code
                sb.AppendFormat("{0}\n", m.Definition.Code);

                // output buffer?
                if (n.OutBuffer)
                {
                    if (m.Definition.OutChannels >= 1) sb.AppendFormat("v[i++]=l;\n");
                    if (m.Definition.OutChannels >= 2) sb.AppendFormat("v[i++]=r;\n");
                }

                // distribute to dependent parameters
                foreach (var n2 in nodes)
                {
                    var m2 = n2.Mod;
                    for (int i2 = 1; i2 < m2.Inputs.Count(); i2++)
                    {
                        if (m2.Inputs[i2] == m)
                        {
                            var offs = (n2.ArrayPos + i2 - 1)-(n.ArrayPos+n.ArrayCount);
                            sb.AppendFormat("v[i+{0}]=l;\n", offs);
                        }
                    }
                }
              
                lastNode = n;
            }

            code = sb.ToString();
        }

        public static string GenerateAllCode(IList<Graph> graphs)
        {
            string synth = Properties.Resources.Synth;

            var values = new StringBuilder();
            var code = new StringBuilder();

            for (int i = 0; i < graphs.Count; i++)
            {
                string v, c;
                graphs[i].GenerateCode(out v, out c);

                if (i > 0) values.Append(",");
                values.Append(v);

                if (i > 0) code.Append("c++; ");
                code.AppendFormat("v=state[c]; i=0; ");
                code.Append(c);
                code.Append("ll+=l; rr+=r; ");
            }

            synth = synth.Replace("//!VALUES", values.ToString());
            synth = synth.Replace("//!CODE", code.ToString());

            return synth;
        }

        // for debugging
        public override string ToString() { return Name + " (" + Modules.Count + ")"; }

        private class ModuleNode
        {
            public Module Mod;
            public int ArrayPos;
            public int ArrayCount;
            public bool OutBuffer;
            //public int[] Outputs;
        }

        private void MakeNodes(Module mod, IList<ModuleNode> list)
        {
            list.Add(new ModuleNode { Mod = mod });

            if (mod.Definition.InChannels > 0 && mod.Inputs[0] == null)
                throw new ModelException("Input of {0} is unused", mod.Name);

            foreach (var imod in mod.Inputs.Where(m => m != null))
                if (!list.Any(mn => mn.Mod == imod))
                    MakeNodes(imod, list);
        }
    }


    public class ModelException : Exception
    {
        public ModelException(string message) : base(message) { }
        public ModelException(string format, params object[] args) : base(String.Format(format, args)) { }
    };


}
