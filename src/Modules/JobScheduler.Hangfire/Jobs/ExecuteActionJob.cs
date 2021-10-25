using System;
using System.Collections;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.ExpressApp.Model;
using Hangfire.Server;
using Microsoft.Extensions.DependencyInjection;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.CollectionSourceExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Jobs {
    [JobProvider]
    public class ExecuteActionJob {
        
        public BlazorApplication Application { get; }

        public ExecuteActionJob() {
        }

        public ExecuteActionJob(BlazorApplication application) => Application = application;

        [JobProvider]
        public async Task Execute(PerformContext context) {
	        var containerInitializer = Application.ServiceProvider.GetService<IValueManagerStorageContainerInitializer>();
	        // if (((IValueManagerStorageAccessor) containerInitializer)?.Storage == null) {
		        // containerInitializer.Initialize();
	        // }
            await Observable.Start(() => {
                
                using var objectSpace = Application.CreateObjectSpace();
                var jobId = context.JobId();
                var job = objectSpace.GetObjectsQuery<BusinessObjects.ExecuteActionJob>().First(actionJob => actionJob.Id==jobId);
                var modelView = Application.Model.Views[job.View.Name];
                CreateView(job, modelView).Subscribe();
                var newView = Application.NewView(modelView.ViewType(),modelView.AsObjectView.ModelClass.TypeInfo.Type);
                var window = Application.CreateViewWindow();
                window.SetView(newView);
                var action = window.Action(job.Action.Name);
                action.DoTheExecute();
            });
        }

        private IObservable<View> CreateView(BusinessObjects.ExecuteActionJob job, IModelView modelView) 
            => Application.WhenViewCreating()
                .Select(t => {
                    var objectType = modelView.AsObjectView?.ModelClass.TypeInfo.Type;
                    var space = Application.CreateObjectSpace(objectType);
                    var collectionSource = Application.CreateCollectionSource(space, objectType, modelView.Id);
                    t.e.View = new ListView((IModelListView) modelView, collectionSource, Application, job.SelectedObjectsCriteria);
                    return t.e.View;
                }).FirstAsync().IgnoreElements();
    }

    class ListView:DevExpress.ExpressApp.ListView {
        private readonly string _selectedObjectsCriteria;

        public ListView(IModelListView modelListView, CollectionSourceBase collectionSource, XafApplication application,
            string selectedObjectsCriteria) : base(modelListView, collectionSource, application, true) {
            _selectedObjectsCriteria = selectedObjectsCriteria;
        }
        public override IList SelectedObjects => CollectionSource.Objects().Where(o =>ObjectSpace.IsObjectFitForCriteria(CriteriaOperator.Parse(_selectedObjectsCriteria),o) ).ToArray();
    }
}