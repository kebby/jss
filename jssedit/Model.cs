using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace jssedit
{
    /// <summary>
    /// Definition for a synth module
    /// </summary>
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
    public class Module
    {
        /// <summary>
        /// Reference to module definition
        /// </summary>
        public readonly ModuleDefinition Definition;

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
            Definition = def;

            Params = new float[def.ParamNames.Length];
            if (def.ParamDefaults != null)
                for (int i = 0; i < Math.Min(Params.Length, def.ParamDefaults.Length); i++)
                    Params[i] = def.ParamDefaults[i];

            Inputs = new Module[Params.Length + 1];
            Name = def.Name.Substring(def.Name.LastIndexOf('/')+1);
        }

        // for debugging
        public override string ToString() { return Name + ": " + Definition; }
    }

    /// <summary>
    /// Module instance graph
    /// </summary>
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
            if (!Modules.Contains(module)) throw new ModelException("Module not in graph");

            // disconnect from everything
            foreach (var m in Modules)
            {
                for (int i=0; i<m.Inputs.Length; i++)
                    if (m.Inputs[i] == module)
                        m.Inputs[i] = null;
            }

            // ... and before anyone does anything funny...
            Array.Clear(module.Inputs, 0, module.Inputs.Length);

            Modules.Remove(module);
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

        // for debugging
        public override string ToString() { return Name + " (" + Modules.Count + ")"; }
    }


    public class ModelException : Exception
    {
        public ModelException(string message) : base(message) { }
        public ModelException(string format, params object[] args) : base(String.Format(format, args)) { }
    };


}
