using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using JetBrains.Annotations;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.NonPersistentObjects;
using Xpand.Extensions.XAF.TypesInfoExtensions;

namespace Xpand.XAF.Modules.SequenceGenerator{
    [PublicAPI]
    public interface ISequenceStorage{
        string Name{ get; set; }
        string CustomSequence{ get; set; }
        long NextSequence{ get; set; }
        string ReleasedSequences{ get; set; }
        string SequenceMember{ get; set; }
    }

    
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SequenceStorageKeyNameAttribute:Attribute{
        public Type Type{ get; }

        public SequenceStorageKeyNameAttribute(Type type){
            Type = type;
        }

        public static Type FindConsumer(Type sequenceType){
            return XafTypesInfo.Instance.PersistentTypes.SelectMany(info => info
                .FindAttributes<SequenceStorageKeyNameAttribute>()
                .Where(attribute => attribute.Type == sequenceType).Select(attribute => info.Type)).FirstOrDefault()??sequenceType;
        }

    }
    [DeferredDeletion(false)][DefaultProperty(nameof(Name))][ImageName("PageSetup")]
    public class SequenceStorage : XPBaseObject, ISequenceStorage,IObjectSpaceLink{
        private long _nextSequence;
        public SequenceStorage(Session session)
            : base(session){
        }
        
        string _name;
        
        [Size(255)][Key]
        [Browsable(false)]
        public string Name{
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }
        
        string _customSequence;

        protected override void OnLoaded(){
            base.OnLoaded();
            if (!(Session is ExplicitUnitOfWork)){
                var sequenceStorageName = Name;
                if (sequenceStorageName != null){
                    var modelClass =CaptionHelper.ApplicationModel.BOModel.GetClass(SequenceStorageKeyNameAttribute.FindConsumer(XafTypesInfo.Instance.FindTypeInfo(sequenceStorageName).Type));
                    Type = new ObjectType(modelClass.TypeInfo.Type){Name = modelClass.Caption};
                    Member=new ObjectString(SequenceMember){Caption = modelClass.GetMemberCaption(SequenceMember)};
                    var customSequence = CustomSequence;
                    if (customSequence!=null){
                        modelClass =CaptionHelper.ApplicationModel.BOModel.GetClass(XafTypesInfo.Instance.FindTypeInfo(customSequence).Type);
                        CustomType=new ObjectType(modelClass.TypeInfo.Type){Name = modelClass.Caption};
                    }
                }   
            }
        }

        [Size(255)][Browsable(false)]
        public string CustomSequence{
            get => _customSequence;
            set => SetPropertyValue(nameof(CustomSequence), ref _customSequence, value);
        }

        ObjectType _type;
        [DataSourceProperty(nameof(Types))][NonPersistent][RuleRequiredField][ImmediatePostData]
        public ObjectType Type{
            get => _type;
            set => SetPropertyValue(nameof(Type), ref _type, value);
        }

        ObjectType _customType;
        [DataSourceProperty(nameof(CustomTypes))][NonPersistent]
        public ObjectType CustomType{
            get => _customType;
            set => SetPropertyValue(nameof(CustomType), ref _customType, value);
        }

        
        [Browsable(false)]
        public List<ObjectType> Types=>CaptionHelper.ApplicationModel.BOModel
            .Where(_ => typeof(IXPObject).IsAssignableFrom(_.TypeInfo.Type)&&_.TypeInfo.IsPersistent)
            .Select(_ => new ObjectType(_.TypeInfo.Type){Name = _.Caption})
            .ToList();

        [Browsable(false)]
        public List<ObjectType> CustomTypes=>CaptionHelper.ApplicationModel.BOModel
            .Where(_ =>Type!=null&&_.TypeInfo.Type!=Type.Type&& _.TypeInfo.Type.IsAssignableFrom(Type.Type))
            .Where(_ => typeof(IXPObject).IsAssignableFrom(_.TypeInfo.Type))
            .Select(_ => new ObjectType(_.TypeInfo.Type){Name = _.Caption}).ToList();

        public override string ToString(){
            return $"{Name}";
        }

        [Browsable(false)]
        public List<ObjectString> Members => Type == null ? new List<ObjectString>()
            : Type.Type.ToTypeInfo().Members.Where(info =>info.IsPublic&& info.MemberType == typeof(long)).Select(info =>
                new ObjectString(info.Name) {
                    Caption = CaptionHelper.ApplicationModel.BOModel.GetClass(info.Owner.Type).GetMemberCaption(info.Name)
                }).ToList();
        
        ObjectString _objectMember;
        [RuleRequiredField][NonPersistent][DataSourceProperty(nameof(Members))]
        public ObjectString Member{
            get => _objectMember;
            set => SetPropertyValue(nameof(Member), ref _objectMember, value);
        }
        string _sequenceMember;
        [RuleRequiredField][Browsable(false)]
        public string SequenceMember{
            get => _sequenceMember;
            set => SetPropertyValue(nameof(SequenceMember), ref _sequenceMember, value);
        }

        public long NextSequence{
            get => _nextSequence;
            set => SetPropertyValue(nameof(NextSequence), ref _nextSequence, value);
        }

        string _releasedSequences;
        

        [VisibleInDetailView(false)]
        [Size(-1)]
        public string ReleasedSequences{
            get => _releasedSequences;
            set => SetPropertyValue(nameof(ReleasedSequences), ref _releasedSequences, value);
        }

        [Browsable(false)]
        public IObjectSpace ObjectSpace{ get; set; }
    }

}