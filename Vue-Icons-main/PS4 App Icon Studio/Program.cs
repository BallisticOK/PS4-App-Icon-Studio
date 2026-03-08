namespace Vue_Icons
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Keep DPI behavior stable for this fixed-layout WinForms UI.
            Application.SetHighDpiMode(HighDpiMode.DpiUnawareGdiScaled);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
