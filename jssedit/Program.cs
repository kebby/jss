using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text;

namespace jssedit
{
   
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            // test stuff
            var json = LoadTextWithoutComments("..\\..\\data\\modules.json");
            ModuleDefinition.LoadRegistry(json);

            TestGraph();
           
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }


        /// <summary>
        /// Test the graph functions
        /// </summary>
        static void TestGraph()
        {

            // test code

            // make a new graph..
            var graph = new Graph
            {
                Name = "Test",
            };

            // add some stuff into the graph...
            var osc = graph.AddModule("Oscillator/Sawtooth");
            var pan = graph.AddModule("Pan");

            // create topology
            graph.Connect(osc, pan);
            graph.Connect(pan, graph.Out);

            // destroy stuff
            graph.RemoveModule(pan);

            // do something wrong
            try
            {
                // this won't work, connecting mono module to stereo one
                graph.Connect(osc, graph.Out);
                throw new InvalidOperationException("that shouldn't have worked");
            }
            catch (ModelException me)
            {
                Trace.WriteLine(me.Message);                    
            }

            // make it all ok again
            pan = graph.AddModule("Pan");
            graph.Connect(osc, pan);
            graph.Connect(pan, graph.Out);

            string code = graph.GenerateCode();
            Trace.WriteLine(code);
        }


        /// <summary>
        /// Load text file and strip C++ style comments (warning: won't stop removing comments from string constants)
        /// </summary>
        /// <param name="filename">File to load</param>
        /// <returns>Loaded text sans comments</returns>
        static string LoadTextWithoutComments(string filename)
        {
            using (var reader = new StreamReader(filename))
            {
                var sb = new StringBuilder();

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var cpos = line.IndexOf("//");
                    if (cpos >= 0)
                        line = line.Substring(0, cpos);
                    if (line.Length > 0)
                        sb.AppendLine(line);
                }

                return sb.ToString();
            }
        }
    }
}
