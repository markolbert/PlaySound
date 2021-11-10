using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Media;
using System.IO;

namespace PlaySound
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [ STAThread ]
        static void Main()
        {
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Form1());

            var args = Environment.GetCommandLineArgs();

            if( args.Length == 1 ) return;

            var soundFile = args[ 1 ];

            if( !File.Exists( soundFile ) )
            {
                soundFile = Path.Combine( Environment.CurrentDirectory, soundFile );
                if( !File.Exists( soundFile ) ) soundFile = null;
            }

            if( !String.IsNullOrEmpty( soundFile ) )
            {
                try
                {
                    new SoundPlayer( soundFile ).PlaySync();
                }
                catch
                {
                }
            }
        }
    }
}
