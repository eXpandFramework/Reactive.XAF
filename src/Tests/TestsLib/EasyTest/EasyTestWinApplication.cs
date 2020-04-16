using DevExpress.EasyTest.Framework;

namespace Xpand.TestsLib.EasyTest{
    public class EasyTestWinApplication : TestApplication{
        public static EasyTestWinApplication New(string physicalPath, int port=4100){
            Instance=new EasyTestWinApplication(physicalPath, port);
            return Instance;
        }

        public static EasyTestWinApplication Instance{ get; private set; }

        private EasyTestWinApplication(string fileName, int port=4100){
            this.AddAttribute("FileName", fileName);
            this.AddAttribute("CommunicationPort", port.ToString());
        }
    }
}