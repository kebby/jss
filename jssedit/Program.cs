using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text;
using System.Resources;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

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

            var graph = TestGraph();
           
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var form = new Form1();
            form.SetGraph(graph);
            Application.Run(form);
        }


        /// <summary>
        /// Test the graph functions
        /// </summary>
        static Graph TestGraph()
        {

            // test code

            // make a new graph..
            var graph = new Graph
            {
                Name = "Test",
            };

            // add some stuff into the graph...
            var osc = graph.AddModule("Oscillator/Sawtooth");
            osc.X = 20; osc.Y = 50;
            var pan = graph.AddModule("Pan");
            pan.X = 120; pan.Y = 70;
            graph.Out.X = 220; graph.Out.Y = 50;

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
            pan.X = 120; pan.Y = 30;

            string code = graph.GenerateCode();
            Trace.WriteLine(code);

            // save test
            using (var file = new FileStream("c:\\test\\bla.dat",FileMode.Create))
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(file, graph);
            }

            // load test
            using (var file = new FileStream("c:\\test\\bla.dat", FileMode.Open))
            {     
                var formatter = new BinaryFormatter();
                var graph2 = formatter.Deserialize(file) as Graph;
                graph2.Modules[1].Name = "lol";
            }
          

            return graph;
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
