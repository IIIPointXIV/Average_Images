using System;
using System.Windows.Forms;

namespace Average_Images
{
    public class Program
    {
        public static Form1 form = new Form1();
        [STAThread]
        static void Main(string[] args)
        {
            form.RunForm(args[0], (args.Count() > 1 ? bool.Parse(args[1]) : true));
        }
    }
}