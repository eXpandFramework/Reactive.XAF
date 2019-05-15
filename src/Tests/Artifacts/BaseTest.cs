using System;
using AppDomainToolkit;
using IDisposable = System.IDisposable;

namespace Tests.Artifacts{
    
    public abstract class BaseTest : IDisposable{
        
        protected readonly IAppDomainContext AppDomainCtx;
        protected AppDomain Domain;

        protected BaseTest(){
            AppDomainCtx = AppDomainContext.Create();
            Domain = AppDomainCtx.Domain;
        }

        public virtual void Dispose(){
            AppDomainCtx.Dispose();
        }

        
    }
}