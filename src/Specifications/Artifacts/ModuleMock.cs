using System;
using DevExpress.ExpressApp;
using Moq;

namespace DevExpress.XAF.Agnostic.Specifications.Artifacts{
    sealed class ModuleMock:Mock<ModuleBase>{
        public ModuleMock(params Type[] requiredModuleTypes){
            CallBase = true;
            Object.RequiredModuleTypes.AddRange(requiredModuleTypes);
        }

    }
}