using System;
using DevExpress.EasyTest.Framework;
using Fasterflect;

namespace Xpand.TestsLib.EasyTest.Commands{
    public abstract class EasyTestCommand:Command{
        public new bool ExpectException{ get; set; }
        public bool SuppressExceptions{
            get => Version.Parse(AssemblyInfo.Version) >= new Version(19, 2, 7) && (bool) this.GetPropertyValue("SuppressException");
            set => throw new NotImplementedException();
        }

        protected sealed override void InternalExecute(ICommandAdapter adapter){
            var suppressException = SuppressExceptions;
            try {
                ExecuteCore(adapter);
                if(ExpectException) {
                    throw new EasyTeCommandException(this,new ExpectedExceptionCommandException(StartPosition));
                }
            }
            catch(AdapterOperationException e) {
                if(suppressException) {
                    Logger.Instance.Exception(e);
                }
                else {
                    if(!ExpectException) {
                        throw new EasyTeCommandException(this,e);
                    }
                }
            }
            catch(CommandException e) {
                if(suppressException) {
                    Logger.Instance.Exception(e);
                } 
                else {
                    if(!ExpectException) {
                        throw new EasyTeCommandException(this,e);
                    }
                }
            }
        }

        protected abstract void ExecuteCore(ICommandAdapter adapter);
        public override string ToString(){
            return $"Name:{GetType().Name}, MainParameter:{Parameters.MainParameter?.Value}, ExtraParameter:{Parameters.ExtraParameter?.Value}";
        }
    }

    public class EasyTeCommandException:Exception{
        public EasyTeCommandException(EasyTestCommand command, Exception innerException) : base(command.ToString(), innerException){
        }
    }

    public class Parameter:DevExpress.EasyTest.Framework.Parameter{
        public Parameter(string line) : base(line.StartsWith(" ")?line:$" {line}", new PositionInScript(0)){
        }

        public Parameter(string name, string paramValue, bool isEqual=true) : base(name, paramValue, isEqual, new PositionInScript(0)){
        }
    }

}