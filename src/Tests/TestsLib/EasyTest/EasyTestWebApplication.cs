using DevExpress.EasyTest.Framework;

namespace Xpand.TestsLib.EasyTest{
    public class EasyTestWebApplication : TestApplication{
        private EasyTestWebApplication(string physicalPath, int port){
            IgnoreCase = true;
            this.AddAttribute("DontRestartIIS", "true");
            this.AddAttribute("UseIISExpress", "true");
            this.AddAttribute("PhysicalPath", physicalPath);
            this.AddAttribute("URL", $"http://localhost:{port}/default.aspx");
        }

        public static EasyTestWebApplication Instance{ get; private set; }

        public static EasyTestWebApplication New(string physicalPath, int port){
            Instance=new EasyTestWebApplication(physicalPath, port);
            return Instance;
        }

    }
}