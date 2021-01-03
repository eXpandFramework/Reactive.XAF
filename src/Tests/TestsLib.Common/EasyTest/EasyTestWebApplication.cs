using DevExpress.EasyTest.Framework;

namespace Xpand.TestsLib.Common.EasyTest{
    public class EasyTestWebApplication : TestApplication{
        private EasyTestWebApplication(string physicalPath, int port, bool useIisExpress){
            IgnoreCase = true;
            this.AddAttribute("DontRestartIIS", "true");
            this.AddAttribute("UseIISExpress", useIisExpress.ToString().ToLower());
            this.AddAttribute("PhysicalPath", physicalPath);
            this.AddAttribute("URL", $"http://localhost:{port}");
        }

        public static EasyTestWebApplication Instance{ get; private set; }
        
        public static EasyTestWebApplication New(string physicalPath, int port,bool useIisExpress=true){
            Instance=new EasyTestWebApplication(physicalPath, port,useIisExpress);
            return Instance;
        }

    }
}