namespace Xpand.XAF.ModelEditor.ModelDifference {
    public class AspectInfo {
        public AspectInfo(string xml, string aspectName) {
            Xml = xml;
            AspectName = aspectName;
        }

        public string Xml { get; set; }
        public string AspectName { get; set; }
    }
}