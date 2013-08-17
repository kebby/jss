using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using NAudio.Wave;
using smnetjs;

namespace jssedit
{
    public class Preview : WaveProvider32, IDisposable
    {
        public Preview()
        {
            Runtime.OnScriptError += (a, b) =>
            {
                Trace.WriteLine("SCRIPT ERROR:\n" + a.Name + "(" + b.LineNumber + "): " + b.Message + " (near " + b.Token + ")");
            };
        }


        public void Dispose()
        {
            Stop();

            if (Script != null)
            {
                Script.Dispose();
                Script = null;
            }

            Runtime.Dispose();
        }

        public bool SetCode(string code)
        {
            Stop();
            Trace.WriteLine(code);
            var script = Runtime.InitScript("myScriptName.js", typeof(MyGlobalObject));

            if (script.Compile(code) && script.Execute())
            {
                Script = script;
                return true;
            }
            else
            {
                script.Dispose();
                return false;
            }

        }

        public void Stop()
        {
            if (Out != null)
            {
                Out.Stop();
                Out.Dispose();
                Out = null;
            }
        }

        public void Play()
        {
            Stop();
            SetWaveFormat(44100, 2); // 16kHz mono
             
            Out = new WaveOut();
            Out.Init(this);
            Out.Play();
            
        }

        public override int Read(float[] buffer, int offset, int sampleCount)
        {
           
            if (Script == null) return sampleCount;

            var watch = Stopwatch.StartNew();
            var res = Script.CallFunction<float[]>("render2", sampleCount/2);
            var time = watch.Elapsed.TotalSeconds;
            var cpu = time * 100 * 44100 / (sampleCount/2);

            Trace.WriteLine("render " + (float)sampleCount/88200 + ": " + time + " -> " + cpu);

            for (int i = 0; i < res.Length; i++ )
                buffer[offset + i] = res[i];
            return res.Length;
        }

        static class MyGlobalObject
        {

            public static void print(string s)
            {
                Trace.WriteLine(s);
            }
        }

        SMRuntime Runtime = new SMRuntime();
        SMScript Script;
        WaveOut Out;
    }
}
