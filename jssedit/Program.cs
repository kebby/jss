using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text;
using System.Resources;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using smnetjs;

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
            var json = LoadTextWithoutComments("..\\..\\..\\data\\modules.json");
            ModuleDefinition.LoadRegistry(json);

            var graph = TestGraph();
           
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Preview = new Preview();

            var form = new Form1();
            form.SetGraph(graph);
            Application.Run(form);

            Preview.Dispose();
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
            osc.Params[0] = 0.01f;
            var env = graph.AddModule("Envelope/Decay");
            env.X = 120; env.Y = 50;
            env.Params[0] = 0.001f;
            var pan = graph.AddModule("Pan");
            pan.X = 220; pan.Y = 70;
            var vol = graph.AddModule("Mul Stereo");
            vol.X = 320; vol.Y = 60;
            vol.Params[0] = 0.3f;
            graph.Out.X = 420; graph.Out.Y = 50;

            // create topology
            graph.Connect(osc, env);
            graph.Connect(env, pan);
            graph.Connect(pan, vol);
            graph.Connect(vol, graph.Out);

          

            string code = Graph.GenerateAllCode(new[] { graph });
            Trace.WriteLine(code);

            var ms = new MemoryStream();
            var ds = new DeflateStream(ms, CompressionLevel.Optimal, true);
            var bytes = Encoding.ASCII.GetBytes(code);
            ds.Write(bytes, 0, bytes.Length);
            ds.Close();

            Trace.WriteLine("code size: " + bytes.Length + ", deflated: " + ms.Length);

            ms.Close();
         
            return graph;
        }


        static public Preview Preview;

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
