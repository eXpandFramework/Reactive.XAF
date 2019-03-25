using System;
using DevExpress.ExpressApp;
using Moq;

namespace Xpand.XAF.Agnostic.Tests.Artifacts{
    sealed class ModuleMock:Mock<ModuleBase>{
        public ModuleMock(params Type[] requiredModuleTypes){
            CallBase = true;
            Object.RequiredModuleTypes.AddRange(requiredModuleTypes);
        }

    }
}