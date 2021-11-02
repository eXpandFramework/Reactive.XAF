// using System;
// using System.Collections.Generic;
// using System.ComponentModel;
// using System.Linq;
// using DevExpress.ExpressApp;
// using DevExpress.ExpressApp.Editors;
// using DevExpress.ExpressApp.Utils;
// using DevExpress.Persistent.Base;
// using DevExpress.Persistent.Validation;
// using DevExpress.Xpo;
// using Xpand.Extensions.XAF.NonPersistentObjects;
// using Xpand.Extensions.XAF.Xpo.ValueConverters;
//
// namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.Email.BusinessObjects {
//     [MapInheritance(MapInheritanceType.OwnTable)]
//     public class Email:Hangfire.Notification.BusinessObjects.NotificationChannel {
//         public Email(Session session) : base(session) { }
//
//         string _criteria;
//         private ObjectType _type;
//
//         [RuleRequiredField]
//         [DataSourceProperty(nameof(Types))]
//         [Size(SizeAttribute.Unlimited)]
//         [ValueConverter(typeof(ObjectTypeValueConverter))]
//         [Persistent]
//         public ObjectType Type {
//             get => _type;
//             set {
//                 if (SetPropertyValue(nameof(Type), ref _type, value)) {
//
//                     OnChanged(nameof(Criteria));
//                 }
//             }
//         }
//         
//         [Browsable(false)]
//         public IList<ObjectType> Types => CaptionHelper.ApplicationModel.EmailModel().EmailTypes
//             .Select(emailType => new ObjectType(emailType.Type.TypeInfo.Type){Name = emailType.Type.Caption})
//             .ToArray();
//
//         [CriteriaOptions(nameof(UserType))]
//         [EditorAlias(EditorAliases.CriteriaPropertyEditor), Size(SizeAttribute.Unlimited)]
//         public string Criteria {
//             get => _criteria;
//             set => SetPropertyValue(nameof(Criteria), ref _criteria, value);
//         }
//
//         [Browsable(false)]
//         public Type UserType => SecuritySystem.UserType;
//     }
// }